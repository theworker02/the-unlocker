using System.Text.Json;
using System.Diagnostics;
using TheUnlocker.AI;
using TheUnlocker.Graph;
using TheUnlocker.Modding;
using TheUnlocker.PackageManager;
using TheUnlocker.Protocol;
using TheUnlocker.Workspaces;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "init":
            Require(args, 3, "unlocker-mod init <directory> <mod-id>");
            ModTemplateScaffolder.Create(args[1], args[2]);
            Console.WriteLine($"Created mod template: {args[1]}");
            return 0;

        case "validate":
            Require(args, 2, "unlocker-mod validate <mod-directory>");
            Validate(args[1]);
            return 0;

        case "package":
        case "pack":
            Require(args, 3, "unlocker-mod package <mod-project-directory> <output-directory>");
            Console.WriteLine($"Created package: {ModPackager.Package(args[1], args[2])}");
            return 0;

        case "install":
            Require(args, 3, "unlocker-mod install <package.zip> <mods-directory>");
            Console.WriteLine(new ModInstaller(args[2], Path.Combine(Path.GetDirectoryName(args[2]) ?? args[2], "Quarantine")).Install(args[1]));
            return 0;

        case "resolve":
            Require(args, 2, "unlocker-mod resolve <mods-directory>");
            Resolve(args[1]);
            return 0;

        case "generate-plan":
            Require(args, 2, "unlocker-mod generate-plan <description>");
            Console.WriteLine(new ModGenerator().GenerateSafePromptPlan(string.Join(' ', args.Skip(1))));
            return 0;

        case "workspace":
            Require(args, 4, "unlocker-mod workspace <directory> <name> <game-id>");
            new WorkspaceService().Create(args[1], new UnlockerWorkspace { Name = args[2], GameId = args[3] });
            Console.WriteLine($"Created workspace: {args[1]}");
            return 0;

        case "lock":
            Require(args, 3, "unlocker-mod lock <mods-directory> <lockfile-path>");
            WriteLockFile(args[1], args[2]);
            return 0;

        case "export-modpack":
            Require(args, 3, "unlocker-mod export-modpack <workspace-directory> <output.zip>");
            Console.WriteLine($"Exported modpack: {new WorkspaceService().ExportModpack(args[1], args[2])}");
            return 0;

        case "import-modpack":
            Require(args, 3, "unlocker-mod import-modpack <modpack.zip> <target-directory>");
            Console.WriteLine($"Imported modpack: {new WorkspaceService().ImportModpack(args[1], args[2])}");
            return 0;

        case "graph":
            Require(args, 3, "unlocker-mod graph <mods-directory> <output.mmd>");
            WriteGraph(args[1], args[2]);
            return 0;

        case "protocol-reg":
            Require(args, 3, "unlocker-mod protocol-reg <theunlocker.exe> <output.reg>");
            File.WriteAllText(args[2], new ProtocolRegistration().CreateRegistryFileContent(args[1]));
            Console.WriteLine($"Wrote protocol registration file: {args[2]}");
            return 0;

        case "run-registry":
            Console.WriteLine("Run the local registry emulator with:");
            Console.WriteLine("dotnet run --project .\\TheUnlocker.Registry.Server\\TheUnlocker.Registry.Server.csproj");
            return 0;

        case "doctor":
            Doctor(args.Length > 1 ? args[1] : Directory.GetCurrentDirectory());
            return 0;

        case "sign":
            Require(args, 3, "unlocker-mod sign <mod-output-directory> <private-key.pem>");
            ModSigner.SignManifest(args[1], args[2]);
            Console.WriteLine("Signed manifest.");
            return 0;

        case "verify-signature":
            Require(args, 3, "unlocker-mod verify-signature <mod-output-directory> <public-key.pem>");
            Console.WriteLine(ModSigner.VerifyManifest(args[1], args[2]) ? "Signature is valid." : "Signature is invalid.");
            return 0;

        case "sign-package":
            Require(args, 3, "unlocker-mod sign-package <package.zip> <private-key.pem>");
            Console.WriteLine($"Package signature: {SignPackage(args[1], args[2])}");
            return 0;

        case "verify-package":
            Require(args, 4, "unlocker-mod verify-package <package.zip> <signature.json> <public-key.pem>");
            Console.WriteLine(VerifyPackage(args[1], args[2], args[3]) ? "Package signature is valid." : "Package signature is invalid.");
            return 0;

        case "keys":
            Require(args, 3, "unlocker-mod keys <output-directory> <publisher-id>");
            Console.WriteLine($"Public key: {ModSigner.GenerateKeys(args[1], args[2])}");
            return 0;

        case "rotate-key":
            Require(args, 4, "unlocker-mod rotate-key <output-directory> <publisher-id> <previous-public-key.pem>");
            Console.WriteLine($"New public key: {ModSigner.RotateKeys(args[1], args[2], args[3])}");
            return 0;

        case "revoke-key":
            Require(args, 5, "unlocker-mod revoke-key <output-directory> <publisher-id> <key-id> <reason>");
            Console.WriteLine($"Revocation record: {ModSigner.RevokeKey(args[1], args[2], args[3], string.Join(' ', args.Skip(4)))}");
            return 0;

        case "publish":
            Require(args, 4, "unlocker-mod publish <package.zip> <repository-index.json> <download-url>");
            Publish(args[1], args[2], args[3]);
            return 0;

        default:
            if (args.Length >= 2)
            {
                Console.WriteLine($"Created package: {ModPackager.Package(args[0], args[1])}");
                return 0;
            }

            PrintUsage();
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

static void Validate(string modDirectory)
{
    var manifestPath = Path.Combine(modDirectory, "mod.json");
    var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidOperationException("mod.json could not be read.");
    var validationDirectory = modDirectory;
    if (!File.Exists(Path.Combine(validationDirectory, manifest.EntryDll)) && Directory.EnumerateFiles(modDirectory, "*.csproj").Any())
    {
        BuildProject(modDirectory);
        validationDirectory = Path.Combine(modDirectory, "bin", "Debug", "net8.0-windows");
    }

    var result = new ModManifestValidator().Validate(manifest, validationDirectory, [manifest.Id]);
    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"WARN {warning}");
    }

    if (!result.IsValid)
    {
        throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors));
    }

    Console.WriteLine("Manifest is valid.");
}

static void BuildProject(string projectDirectory)
{
    var process = Process.Start(new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "build",
        WorkingDirectory = projectDirectory,
        UseShellExecute = false,
        RedirectStandardError = true,
        RedirectStandardOutput = true
    }) ?? throw new InvalidOperationException("Could not start dotnet build.");
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd());
    }
}

static void Publish(string packagePath, string indexPath, string downloadUrl)
{
    var packageName = Path.GetFileNameWithoutExtension(packagePath);
    var parts = packageName.Split('-', StringSplitOptions.RemoveEmptyEntries);
    var id = parts.Length > 1 ? string.Join('-', parts[..^1]) : packageName;
    var version = parts.LastOrDefault() ?? "1.0.0";
    var index = File.Exists(indexPath)
        ? JsonSerializer.Deserialize<ModRepositoryIndex>(File.ReadAllText(indexPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ModRepositoryIndex()
        : new ModRepositoryIndex();

    index.Mods.RemoveAll(mod => mod.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    index.Mods.Add(new ModRepositoryEntry
    {
        Id = id,
        Name = id,
        Version = version,
        DownloadUrl = downloadUrl,
        Sha256 = ComputeSha256(packagePath),
        Changelog = "Published from unlocker-mod CLI."
    });

    File.WriteAllText(indexPath, JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Published {id} {version} to {indexPath}.");
}

static void Resolve(string modsDirectory)
{
    var manifests = ReadManifests(modsDirectory);
    var resolver = new Resolver();
    var problems = resolver.GetProblems(manifests);
    foreach (var problem in problems)
    {
        Console.WriteLine($"WARN {problem}");
    }

    foreach (var id in resolver.Resolve(manifests))
    {
        Console.WriteLine(id);
    }
}

static void WriteLockFile(string modsDirectory, string lockfilePath)
{
    var packages = Directory.EnumerateFiles(modsDirectory, "*.zip", SearchOption.AllDirectories)
        .Concat(Directory.EnumerateFiles(modsDirectory, "*.dll", SearchOption.AllDirectories))
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

    var lockFile = new WorkspaceService().CreateLockFile(packages);
    File.WriteAllText(lockfilePath, JsonSerializer.Serialize(lockFile, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Wrote lockfile: {lockfilePath}");
}

static void WriteGraph(string modsDirectory, string graphPath)
{
    File.WriteAllText(graphPath, new DependencyGraphExporter().ToMermaid(ReadManifests(modsDirectory)));
    Console.WriteLine($"Wrote dependency graph: {graphPath}");
}

static ModManifest[] ReadManifests(string modsDirectory)
{
    return Directory.EnumerateFiles(modsDirectory, "mod.json", SearchOption.AllDirectories)
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        .Select(path => JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
        .Where(manifest => manifest is not null)
        .Cast<ModManifest>()
        .ToArray();
}

static string ComputeSha256(string path)
{
    using var stream = File.OpenRead(path);
    return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
}

static string SignPackage(string packagePath, string privateKeyPath)
{
    var hash = ComputeSha256(packagePath);
    using var rsa = System.Security.Cryptography.RSA.Create();
    rsa.ImportFromPem(File.ReadAllText(privateKeyPath));
    var signature = Convert.ToBase64String(rsa.SignData(File.ReadAllBytes(packagePath), System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1));
    var signaturePath = packagePath + ".signature.json";
    var record = new
    {
        package = Path.GetFileName(packagePath),
        sha256 = hash,
        algorithm = "RSASSA-PKCS1-v1_5-SHA256",
        signature,
        signedAt = DateTimeOffset.UtcNow
    };
    File.WriteAllText(signaturePath, JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
    return signaturePath;
}

static bool VerifyPackage(string packagePath, string signaturePath, string publicKeyPath)
{
    using var document = JsonDocument.Parse(File.ReadAllText(signaturePath));
    var expected = document.RootElement.GetProperty("sha256").GetString();
    var signature = Convert.FromBase64String(document.RootElement.GetProperty("signature").GetString() ?? "");
    using var rsa = System.Security.Cryptography.RSA.Create();
    rsa.ImportFromPem(File.ReadAllText(publicKeyPath));
    return string.Equals(expected, ComputeSha256(packagePath), StringComparison.OrdinalIgnoreCase)
        && rsa.VerifyData(File.ReadAllBytes(packagePath), signature, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
}

static void Doctor(string path)
{
    Console.WriteLine("TheUnlocker doctor");
    Console.WriteLine($"Workspace: {Path.GetFullPath(path)}");
    Console.WriteLine($"dotnet: {(RunCheck("dotnet", "--version") ? "ok" : "missing")}");

    var manifestPath = Path.Combine(path, "mod.json");
    Console.WriteLine($"manifest: {(File.Exists(manifestPath) ? "found" : "missing")}");
    if (File.Exists(manifestPath))
    {
        try
        {
            Validate(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"manifest validation: failed - {ex.Message}");
        }
    }

    Console.WriteLine($"signing keys: {(Directory.EnumerateFiles(path, "*.pem", SearchOption.AllDirectories).Any() ? "found" : "not found")}");
    Console.WriteLine($"packages: {Directory.EnumerateFiles(path, "*.zip", SearchOption.AllDirectories).Count()} zip file(s)");
    Console.WriteLine("registry: use `unlocker-mod run-registry` for a local registry emulator.");
}

static bool RunCheck(string fileName, string arguments)
{
    try
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        process?.WaitForExit(3000);
        return process?.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}

static void Require(string[] args, int count, string usage)
{
    if (args.Length < count)
    {
        throw new InvalidOperationException($"Usage: {usage}");
    }
}

static void PrintUsage()
{
    Console.WriteLine("""
unlocker-mod commands:
  init <directory> <mod-id>
  validate <mod-directory>
  package <mod-project-directory> <output-directory>
  pack <mod-project-directory> <output-directory>
  install <package.zip> <mods-directory>
  resolve <mods-directory>
  sign <mod-output-directory> <private-key.pem>
  verify-signature <mod-output-directory> <public-key.pem>
  sign-package <package.zip> <private-key.pem>
  verify-package <package.zip> <signature.json> <public-key.pem>
  keys <output-directory> <publisher-id>
  rotate-key <output-directory> <publisher-id> <previous-public-key.pem>
  revoke-key <output-directory> <publisher-id> <key-id> <reason>
  publish <package.zip> <repository-index.json> <download-url>
  generate-plan <description>
  workspace <directory> <name> <game-id>
  lock <mods-directory> <lockfile-path>
  export-modpack <workspace-directory> <output.zip>
  import-modpack <modpack.zip> <target-directory>
  graph <mods-directory> <output.mmd>
  protocol-reg <theunlocker.exe> <output.reg>
  run-registry
  doctor [path]
""");
}
