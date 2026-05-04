namespace TheUnlocker.Plugins;

public interface IPlugin
{
    string Name { get; }

    void Initialize();
}
