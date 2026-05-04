using TheUnlocker.Modding;

public sealed class CommandMod : IMod
{
    public string Id => "sample-command";
    public string Name => "Sample Command";
    public Version Version => new(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.CommandPalette.Register("sample.sayHello", "AddMenuItems", () => context.Notifications.Show("Hello from the command palette."));
    }

    public void OnUnload()
    {
    }
}
