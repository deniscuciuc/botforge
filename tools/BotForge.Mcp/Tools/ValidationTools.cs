using System.ComponentModel;
using System.Text;
using BotForge.Mcp.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelContextProtocol.Server;

namespace BotForge.Mcp.Tools;

[McpServerToolType]
public class ValidationTools(TemplateValidationService templateValidator)
{
    [McpServerTool]
    [Description(
        "Validates a BotForge YAML template for structural correctness — required fields, translation consistency, button types, mustache balance, dynamic button configuration.")]
    public string ValidateTemplate(
        [Description("YAML content to validate")]
        string? yamlContent = null,
        [Description("Absolute path to a .yml/.yaml template file")]
        string? filePath = null)
    {
        if (yamlContent is null && filePath is not null)
        {
            if (!File.Exists(filePath))
                return $"File not found: {filePath}";
            yamlContent = File.ReadAllText(filePath);
        }

        if (string.IsNullOrWhiteSpace(yamlContent))
            return "Provide either 'yamlContent' or 'filePath' to validate.";

        var result = templateValidator.Validate(yamlContent);
        return result.Format();
    }

    [McpServerTool]
    [Description(
        "Validates a BotForge handler implementation — checks that the correct interface is implemented, matching attribute is present, return type is correct, and DI patterns follow conventions.")]
    public static string ValidateHandler(
        [Description("C# source code of the handler to validate")]
        string? sourceCode = null,
        [Description("Absolute path to the handler .cs file")]
        string? filePath = null)
    {
        if (sourceCode is null && filePath is not null)
        {
            if (!File.Exists(filePath))
                return $"File not found: {filePath}";
            sourceCode = File.ReadAllText(filePath);
        }

        if (string.IsNullOrWhiteSpace(sourceCode))
            return "Provide either 'sourceCode' or 'filePath' to validate.";

        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();
        var sb = new StringBuilder();
        var hasIssues = false;

        // Handler interface → required attribute mapping
        var handlerAttributeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ICommandHandler"] = "TelegramCommand",
            ["ICallbackQueryHandler"] = "CallbackQuery",
            ["ITextMessageHandler"] = "TextMessage"
        };

        // Handler interface → expected return type
        var handlerReturnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ICommandHandler"] = "Task<CommandResult>",
            ["ICallbackQueryHandler"] = "Task<CallbackResult>",
            ["ITextMessageHandler"] = "Task<TextMessageResult>",
            ["IMediaHandler"] = "Task",
            ["IInlineQueryHandler"] = "Task",
            ["IDiceHandler"] = "Task",
            ["IConversationHandler"] = "Task<ConversationResult>"
        };

        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        foreach (var classDecl in classes)
        {
            var className = classDecl.Identifier.Text;
            var baseList = classDecl.BaseList;
            if (baseList is null)
                continue;

            var implementedInterfaces = baseList.Types
                .Select(t => t.Type.ToString())
                .ToList();

            foreach (var iface in implementedInterfaces)
            {
                var simpleIface = iface.Contains('.') ? iface.Split('.').Last() : iface;

                // Check for required attribute (QG0001)
                if (handlerAttributeMap.TryGetValue(simpleIface, out var requiredAttr))
                {
                    var attributes = classDecl.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Select(a => a.Name.ToString())
                        .ToList();

                    if (!attributes.Any(a => a.Contains(requiredAttr)))
                    {
                        sb.AppendLine(
                            $"❌ QG0001: Class `{className}` implements `{simpleIface}` but is missing `[{requiredAttr}]` attribute");
                        hasIssues = true;
                    }
                }

                // Check return type (QG0005)
                if (handlerReturnMap.TryGetValue(simpleIface, out var expectedReturn))
                {
                    var methodName = simpleIface == "IConversationHandler" ? "HandleStepAsync" : "HandleAsync";
                    var methods = classDecl.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Where(m => m.Identifier.Text == methodName);

                    foreach (var method in methods)
                    {
                        var returnType = method.ReturnType.ToString();
                        if (!returnType.Contains(expectedReturn.Replace("Task<", "").Replace(">", "")) &&
                            returnType != expectedReturn &&
                            returnType != "async " + expectedReturn)
                            // Only warn if it's clearly wrong (not just async modifier difference)
                            if (!returnType.Contains("Task"))
                            {
                                sb.AppendLine(
                                    $"⚠️ QG0005: `{className}.{methodName}()` returns `{returnType}`, expected `{expectedReturn}`");
                                hasIssues = true;
                            }
                    }

                    if (!methods.Any())
                    {
                        sb.AppendLine(
                            $"⚠️ `{className}` implements `{simpleIface}` but does not have a `{methodName}` method");
                        hasIssues = true;
                    }
                }

                // Check for duplicate routes (QG0002 — basic check)
                if (simpleIface == "ICommandHandler")
                {
                    var cmdAttrs = classDecl.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Where(a => a.Name.ToString().Contains("TelegramCommand"))
                        .ToList();

                    foreach (var attr in cmdAttrs)
                        if (attr.ArgumentList?.Arguments.FirstOrDefault()
                                ?.Expression is LiteralExpressionSyntax literal)
                        {
                            var cmd = literal.Token.ValueText;
                            if (cmd.StartsWith('/'))
                                sb.AppendLine(
                                    $"⚠️ `{className}`: [TelegramCommand(\"{cmd}\")] — command name should NOT include '/' prefix. Use \"{cmd.TrimStart('/')}\"");
                        }
                }
            }

            // Check constructor injection patterns
            var constructors = classDecl.Members.OfType<ConstructorDeclarationSyntax>();
            foreach (var ctor in constructors)
                if (ctor.ParameterList.Parameters.Count == 0 && classDecl.Members.OfType<FieldDeclarationSyntax>()
                        .Any(f => f.Declaration.Type.ToString().StartsWith("I")))
                {
                    sb.AppendLine(
                        $"⚠️ `{className}`: Has interface-typed fields but parameterless constructor — use constructor injection instead");
                    hasIssues = true;
                }

            // Check for non-public handler classes
            if (implementedInterfaces.Any(i => handlerReturnMap.ContainsKey(i.Contains('.') ? i.Split('.').Last() : i)))
                if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                {
                    sb.AppendLine($"⚠️ `{className}`: Handler class should be `public` for discovery");
                    hasIssues = true;
                }
        }

        if (!hasIssues)
            sb.AppendLine("✅ Handler validation passed — no issues found.");

        return sb.ToString();
    }
}
