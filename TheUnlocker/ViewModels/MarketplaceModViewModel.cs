using TheUnlocker.Modding;

namespace TheUnlocker.ViewModels;

public sealed class MarketplaceModViewModel
{
    public MarketplaceModViewModel(ModRepositoryEntry entry)
    {
        Id = entry.Id;
        Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.Id : entry.Name;
        Version = entry.Version;
        Description = entry.Description;
        DownloadUrl = entry.DownloadUrl;
        Sha256 = entry.Sha256;
    }

    public string Id { get; }

    public string Name { get; }

    public string Version { get; }

    public string Description { get; }

    public string DownloadUrl { get; }

    public string Sha256 { get; }
}
