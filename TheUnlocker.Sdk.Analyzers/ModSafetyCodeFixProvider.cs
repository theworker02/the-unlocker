using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TheUnlocker.Sdk.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ModSafetyCodeFixProvider)), Shared]
public sealed class ModSafetyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ["TU0001", "TU0002"];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var title = diagnostic.Id == "TU0001"
                ? "Add manifest permission hint"
                : "Add async lifecycle stub";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => diagnostic.Id == "TU0001"
                        ? AddPermissionHintAsync(context.Document, diagnostic, ct)
                        : AddLifecycleStubAsync(context.Document, diagnostic, ct),
                    equivalenceKey: title),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> AddPermissionHintAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
        var statement = token.Parent?.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
        if (statement is null)
        {
            return document;
        }

        var hint = SyntaxFactory.ParseStatement("// mod.json permission hint: declare FileSystem, Network, or ProcessLaunch as appropriate before using this API.\n")
            .WithLeadingTrivia(statement.GetLeadingTrivia());
        var newRoot = root.InsertNodesBefore(statement, [hint]);
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> AddLifecycleStubAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var method = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        var type = method?.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (method is null || type is null)
        {
            return document;
        }

        var stub = SyntaxFactory.ParseMemberDeclaration("""
public System.Threading.Tasks.Task OnLoadAsync(TheUnlocker.Modding.Abstractions.IModContext context, System.Threading.CancellationToken cancellationToken)
{
    return System.Threading.Tasks.Task.CompletedTask;
}
""");
        if (stub is null)
        {
            return document;
        }

        var newType = type.AddMembers(stub);
        var newRoot = root.ReplaceNode(type, newType);
        return document.WithSyntaxRoot(newRoot);
    }
}
