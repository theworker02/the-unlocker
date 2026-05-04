using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using TheUnlocker.Sdk.Analyzers;
using Xunit;

namespace TheUnlocker.Tests;

public sealed class SdkAnalyzerTests
{
    [Fact]
    public async Task Tu0001_reports_direct_file_api_usage()
    {
        var diagnostics = await AnalyzeAsync("""
using System.IO;
public sealed class TestMod
{
    public void Run()
    {
        File.ReadAllText("settings.json");
    }
}
""");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "TU0001");
    }

    [Fact]
    public async Task Tu0002_reports_large_onload_method()
    {
        var body = string.Join(Environment.NewLine, Enumerable.Range(0, 21).Select(i => $"        var value{i} = {i};"));
        var diagnostics = await AnalyzeAsync($$"""
public sealed class TestMod
{
    public void OnLoad()
    {
{{body}}
    }
}
""");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "TU0002");
    }

    private static async Task<IReadOnlyCollection<Diagnostic>> AnalyzeAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.IO.File).Assembly.Location)
        };
        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var analyzer = new ModSafetyAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
