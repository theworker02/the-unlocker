namespace TheUnlocker.Modding;

public interface IModMigration
{
    void Migrate(IModMigrationContext context);
}
