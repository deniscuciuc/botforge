using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TeleForge.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class AddHandlerAttributeCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.MissingHandlerAttribute);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    private static readonly ImmutableDictionary<string, (string AttributeName, string UsingNamespace)>
        InterfaceToAttribute =
            ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, (string, string)>("ICommandHandler",
                    ("TelegramCommand(\"/command\")", "TeleForge.Routing.Attributes")),
                new KeyValuePair<string, (string, string)>("ICallbackQueryHandler",
                    ("CallbackQuery(\"pattern\")", "TeleForge.Routing.Attributes")),
                new KeyValuePair<string, (string, string)>("ITextMessageHandler",
                    ("TextMessage()", "TeleForge.Routing.Attributes")),
                new KeyValuePair<string, (string, string)>("IInlineQueryHandler",
                    ("InlineQuery()", "TeleForge.Routing.Attributes")),
                new KeyValuePair<string, (string, string)>("IMediaHandler",
                    ("MediaMessage(MediaType.Photo)", "TeleForge.Routing.Attributes")),
                new KeyValuePair<string, (string, string)>("ILocationHandler",
                    ("LocationMessage", "TeleForge.Routing.Attributes")),
                new KeyValuePair<string, (string, string)>("IContactHandler",
                    ("ContactMessage", "TeleForge.Routing.Attributes")),
                new KeyValuePair<string, (string, string)>("IPollAnswerHandler",
                    ("PollAnswer", "TeleForge.Routing.Attributes")),
                new KeyValuePair<string, (string, string)>("IChatMemberHandler",
                    ("ChatMember", "TeleForge.Routing.Attributes")),
                new KeyValuePair<string, (string, string)>("IDiceHandler",
                    ("DiceMessage()", "TeleForge.Routing.Attributes"))
            });

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root == null) return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var classDecl = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();
        if (classDecl == null) return;

        var interfaceName = diagnostic.Properties.GetValueOrDefault("InterfaceName");
        if (interfaceName == null)
        {
            // Fallback: extract from message
            var message = diagnostic.GetMessage();
            foreach (var key in InterfaceToAttribute.Keys)
                if (message.Contains(key))
                {
                    interfaceName = key;
                    break;
                }
        }

        if (interfaceName == null || !InterfaceToAttribute.TryGetValue(interfaceName, out var attrInfo))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                $"Add [{attrInfo.AttributeName.Split('(')[0]}] attribute",
                ct => AddAttributeAsync(context.Document, classDecl, attrInfo.AttributeName, ct),
                DiagnosticIds.MissingHandlerAttribute),
            diagnostic);
    }

    private static async Task<Document> AddAttributeAsync(
        Document document,
        ClassDeclarationSyntax classDecl,
        string attributeText,
        CancellationToken ct)
    {
        var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(attributeText));
        var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));

        var newClassDecl = classDecl.AddAttributeLists(attributeList);

        var root = await document.GetSyntaxRootAsync(ct);
        if (root == null) return document;

        var newRoot = root.ReplaceNode(classDecl, newClassDecl);
        return document.WithSyntaxRoot(newRoot);
    }
}
