using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BotForge.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HandlerReturnTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.HandlerReturnType,
        "Handler method should return Task",
        "Handler method '{0}' returns void — consider returning Task for async execution",
        "BotForge.Routing",
        DiagnosticSeverity.Info,
        true,
        "Handler methods should return Task for proper async execution.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    private static readonly ImmutableHashSet<string> HandlerMethodNames =
        ImmutableHashSet.Create("HandleAsync", "HandleStepAsync");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDecl = (MethodDeclarationSyntax)context.Node;
        if (!HandlerMethodNames.Contains(methodDecl.Identifier.Text))
            return;

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl);
        if (methodSymbol == null)
            return;

        if (methodSymbol.ReturnsVoid)
        {
            var diagnostic = Diagnostic.Create(Rule, methodDecl.ReturnType.GetLocation(), methodDecl.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
