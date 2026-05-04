using TheUnlocker.Modding;

public sealed class AssetImporterMod : IMod
{
    public string Id => "sample-asset-importer";
    public string Name => "Sample Asset Importer";
    public Version Version => new(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.AssetImporters.Register(".sampleasset", "Sample Asset Importer");
    }

    public void OnUnload()
    {
    }
}
