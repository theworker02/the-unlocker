using TheUnlocker.Modding;

public sealed class MigrationMod : IMod, IModMigration
{
    public string Id => "sample-migration";
    public string Name => "Sample Migration";
    public Version Version => new(2, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.Notifications.Show("Migration sample loaded.");
    }

    public void OnUnload()
    {
    }

    public void Migrate(IModMigrationContext context)
    {
        if (context.FromVersion < new Version(2, 0, 0))
        {
            var oldMessage = context.Settings.Get("enabledMessage", "Migration sample loaded.");
            context.Settings.Set("message", oldMessage);
            context.Settings.Set("migrationComplete", "true");
            context.Log("Migrated enabledMessage to message.");
        }
    }
}
