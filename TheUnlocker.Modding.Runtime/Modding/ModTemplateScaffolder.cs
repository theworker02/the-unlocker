using System.IO;
using System.Text.Json;

namespace TheUnlocker.Modding;

public static class ModTemplateScaffolder
{
    public static void Create(string outputDirectory, string modId)
    {
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, $"{modId}.csproj"), $"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\TheUnlocker.Modding.Abstractions\TheUnlocker.Modding.Abstractions.csproj" />
  </ItemGroup>
  <ItemGroup>
    <None Update="mod.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
""");
        File.WriteAllText(Path.Combine(outputDirectory, "Mod.cs"), $$"""
using TheUnlocker.Modding;

public sealed class {{ToPascal(modId)}}Mod : IMod
{
    public string Id => "{{modId}}";
    public string Name => "{{ToPascal(modId)}}";
    public Version Version => new(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.Log(Id, "Loaded.");
    }

    public void OnUnload()
    {
    }
}
""");
        File.WriteAllText(Path.Combine(outputDirectory, "mod.json"), JsonSerializer.Serialize(new ModManifest
        {
            Id = modId,
            Name = ToPascal(modId),
            Version = "1.0.0",
            EntryDll = $"{modId}.dll",
            SdkVersion = "1.0.0"
        }, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(outputDirectory, "package.ps1"), "dotnet run --project ..\\TheUnlocker.ModPackager -- package . .\\dist");
    }

    private static string ToPascal(string value)
    {
        return string.Concat(value.Split(['-', '_', ' '], StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}
