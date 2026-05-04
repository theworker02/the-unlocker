namespace TheUnlocker.Extensions;

public enum ExtensionPackageType
{
    GameAdapter,
    UiTheme,
    PackageFormatPlugin,
    ScannerPlugin,
    MarketplacePanel,
    WorkflowAction
}

public sealed class ExtensionPackageListing
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public ExtensionPackageType Type { get; init; }
    public string Version { get; init; } = "1.0.0";
    public string PublisherId { get; init; } = "";
    public string DownloadUrl { get; init; } = "";
    public string[] RequiredPermissions { get; init; } = [];
}

public sealed class ExtensionMarketplaceService
{
    public IReadOnlyList<ExtensionPackageListing> Filter(IEnumerable<ExtensionPackageListing> listings, ExtensionPackageType type)
    {
        return listings.Where(listing => listing.Type == type)
            .OrderBy(listing => listing.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
