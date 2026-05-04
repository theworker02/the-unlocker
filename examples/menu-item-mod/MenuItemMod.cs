using TheUnlocker.Modding;

public sealed class MenuItemMod : IMod
{
    public string Id => "sample-menu-item";
    public string Name => "Sample Menu Item";
    public Version Version => new(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.MenuItems.Add("Sample Menu Action", () => context.Notifications.Show("Sample menu action executed."));
    }

    public void OnUnload()
    {
    }
}
