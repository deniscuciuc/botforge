using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BotForge.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateRouteAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.DuplicateRoute,
        "Duplicate route pattern",
        "Route '{0}' is already registered by another handler",
        "BotForge.Routing",
        DiagnosticSeverity.Error,
        true,
        "Two handlers should not register the same command or callback pattern.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        var commandRoutes = new Dictionary<string, Location>();
        var callbackRoutes = new Dictionary<string, Location>();

        foreach (var tree in context.Compilation.SyntaxTrees)
        {
            var semanticModel = context.Compilation.GetSemanticModel(tree);
            var root = tree.GetRoot(context.CancellationToken);

            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl, context.CancellationToken);
                if (classSymbol == null || classSymbol.IsAbstract)
                    continue;

                foreach (var attr in classSymbol.GetAttributes())
                {
                    var attrName = attr.AttributeClass?.Name;

                    if (attrName == "TelegramCommandAttribute" && attr.ConstructorArguments.Length > 0)
                    {
                        var route = attr.ConstructorArguments[0].Value?.ToString();
                        if (route != null)
                            CheckDuplicate(context, commandRoutes, route, classDecl.Identifier.GetLocation());
                    }
                    else if (attrName == "CallbackQueryAttribute" && attr.ConstructorArguments.Length > 0)
                    {
                        var route = attr.ConstructorArguments[0].Value?.ToString();
                        if (route != null)
                            CheckDuplicate(context, callbackRoutes, route, classDecl.Identifier.GetLocation());
                    }
                }
            }
        }
    }

    private static void CheckDuplicate(
        CompilationAnalysisContext context,
        Dictionary<string, Location> routeMap,
        string route,
        Location location)
    {
        if (routeMap.TryGetValue(route, out var existingLocation))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, route));
            context.ReportDiagnostic(Diagnostic.Create(Rule, existingLocation, route));
        }
        else
        {
            routeMap[route] = location;
        }
    }
}
