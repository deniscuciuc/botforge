using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BotForge.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingHandlerAttributeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MissingHandlerAttribute,
        "Missing handler routing attribute",
        "Class '{0}' implements '{1}' but has no routing attribute",
        "BotForge.Routing",
        DiagnosticSeverity.Warning,
        true,
        "Handler classes that implement a handler interface should have the corresponding routing attribute.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    private static readonly ImmutableDictionary<string, string> HandlerAttributeMap =
        ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, string>("ICommandHandler", "TelegramCommandAttribute"),
            new KeyValuePair<string, string>("ICallbackQueryHandler", "CallbackQueryAttribute"),
            new KeyValuePair<string, string>("ITextMessageHandler", "TextMessageAttribute"),
            new KeyValuePair<string, string>("IInlineQueryHandler", "InlineQueryAttribute"),
            new KeyValuePair<string, string>("IMediaHandler", "MediaMessageAttribute"),
            new KeyValuePair<string, string>("ILocationHandler", "LocationMessageAttribute"),
            new KeyValuePair<string, string>("IContactHandler", "ContactMessageAttribute"),
            new KeyValuePair<string, string>("IPollAnswerHandler", "PollAnswerAttribute"),
            new KeyValuePair<string, string>("IChatMemberHandler", "ChatMemberAttribute"),
            new KeyValuePair<string, string>("IDiceHandler", "DiceMessageAttribute")
        });

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        if (classSymbol == null || classSymbol.IsAbstract)
            return;

        foreach (var iface in classSymbol.AllInterfaces)
            if (HandlerAttributeMap.TryGetValue(iface.Name, out var expectedAttr))
            {
                var hasAttribute = false;
                foreach (var attr in classSymbol.GetAttributes())
                    if (attr.AttributeClass?.Name == expectedAttr)
                    {
                        hasAttribute = true;
                        break;
                    }

                if (!hasAttribute)
                {
                    var diagnostic = Diagnostic.Create(Rule, classDecl.Identifier.GetLocation(),
                        classSymbol.Name, iface.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
    }
}
