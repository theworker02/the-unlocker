using TheUnlocker.Modding;

public sealed class ToolPanelMod : IMod
{
    public string Id => "sample-tool-panel";
    public string Name => "Sample Tool Panel";
    public Version Version => new(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.ToolPanels.Register("Sample Panel");
    }

    public void OnUnload()
    {
    }
}
