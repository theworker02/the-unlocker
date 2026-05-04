using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TheUnlocker.Sdk.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ModSafetyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor UnsafeApiRule = new(
        "TU0001",
        "Unsafe API requires explicit review",
        "Mods should avoid direct use of '{0}' unless the manifest declares and documents the required permission",
        "TheUnlocker",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor LifecycleRule = new(
        "TU0002",
        "Prefer lifecycle interfaces for startup work",
        "IMod.OnLoad should stay small; move staged startup into IModLifecycle or IAsyncModLifecycle when possible",
        "TheUnlocker",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [UnsafeApiRule, LifecycleRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var text = invocation.Expression.ToString();
        if (text.StartsWith("File.", StringComparison.Ordinal)
            || text.StartsWith("Directory.", StringComparison.Ordinal)
            || text.StartsWith("Process.", StringComparison.Ordinal)
            || text.StartsWith("HttpClient.", StringComparison.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(UnsafeApiRule, invocation.GetLocation(), text.Split('.')[0]));
        }
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        if (method.Identifier.Text == "OnLoad" && method.Body?.Statements.Count > 20)
        {
            context.ReportDiagnostic(Diagnostic.Create(LifecycleRule, method.Identifier.GetLocation()));
        }
    }
}
