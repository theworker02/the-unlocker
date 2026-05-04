using TheUnlocker.Modding;

public sealed class SampleUnlockerMod : IMod
{
    public string Id => "sample-unlocker-mod";

    public string Name => "Sample Unlocker Mod";

    public Version Version => new(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.Log(Id, "Loaded.");
    }

    public void OnUnload()
    {
    }
}
