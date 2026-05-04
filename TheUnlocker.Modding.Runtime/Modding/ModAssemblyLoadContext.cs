using System.Reflection;
using System.Runtime.Loader;

namespace TheUnlocker.Modding;

public sealed class ModAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _privateLibDirectory;

    public ModAssemblyLoadContext(string mainAssemblyPath)
        : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
        _privateLibDirectory = Path.Combine(Path.GetDirectoryName(mainAssemblyPath) ?? "", "lib");
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == typeof(IMod).Assembly.GetName().Name)
        {
            return null;
        }

        var privateAssemblyPath = Path.Combine(_privateLibDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(privateAssemblyPath))
        {
            return LoadFromAssemblyPath(privateAssemblyPath);
        }

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
    }
}
