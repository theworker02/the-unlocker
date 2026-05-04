namespace TheUnlocker.Modding;

public interface IMod
{
    string Id { get; }

    string Name { get; }

    Version Version { get; }

    void OnLoad(IModContext context);

    void OnUnload();
}
