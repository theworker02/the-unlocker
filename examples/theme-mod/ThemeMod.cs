using TheUnlocker.Modding;

public sealed class ThemeMod : IMod
{
    public string Id => "sample-theme";
    public string Name => "Sample Theme";
    public Version Version => new(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.Themes.Register("Sample Slate");
    }

    public void OnUnload()
    {
    }
}
