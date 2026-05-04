namespace TheUnlocker.Modding;

public interface IAssetImporterRegistry
{
    IReadOnlyCollection<string> Importers { get; }

    void Register(string extension, string displayName);
}
