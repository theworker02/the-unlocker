using System.Reflection;

namespace TheUnlocker.Plugins;

public sealed class PluginLoader
{
    public IReadOnlyList<IPlugin> Load(string pluginsDirectory)
    {
        Directory.CreateDirectory(pluginsDirectory);
        var plugins = new List<IPlugin>();

        foreach (var dll in Directory.EnumerateFiles(pluginsDirectory, "*.dll"))
        {
            var assembly = Assembly.LoadFrom(dll);
            foreach (var type in assembly.GetTypes().Where(type => typeof(IPlugin).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false }))
            {
                if (Activator.CreateInstance(type) is IPlugin plugin)
                {
                    plugin.Initialize();
                    plugins.Add(plugin);
                }
            }
        }

        return plugins;
    }
}
