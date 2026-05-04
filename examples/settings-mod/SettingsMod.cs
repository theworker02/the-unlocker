using TheUnlocker.Modding;

public sealed class SettingsMod : IMod
{
    public string Id => "sample-settings";
    public string Name => "Sample Settings";
    public Version Version => new(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.Settings.Set("enabledMessage", "Settings-backed sample mod loaded.");
        context.Notifications.Show(context.Settings.Get("enabledMessage", "Settings sample loaded."));
    }

    public void OnUnload()
    {
    }
}
