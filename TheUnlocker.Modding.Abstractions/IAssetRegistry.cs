namespace TheUnlocker.Modding;

public interface IAssetRegistry
{
    IReadOnlyCollection<string> Assets { get; }

    void Register(string assetPath);
}
