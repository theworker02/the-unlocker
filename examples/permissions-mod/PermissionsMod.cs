using TheUnlocker.Modding;

public sealed class PermissionsMod : IMod
{
    public string Id => "sample-permissions";
    public string Name => "Sample Permissions";
    public Version Version => new(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.MenuItems.Add("Permissions Sample", () => context.Notifications.Show("Permission-gated command executed."));
    }

    public void OnUnload()
    {
    }
}
