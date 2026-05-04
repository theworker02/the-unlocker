namespace TheUnlocker.Runtime;

public sealed class Injector
{
    public string PrepareCooperativeLoad(string hostPluginDirectory, string modPackagePath)
    {
        Directory.CreateDirectory(hostPluginDirectory);
        var target = Path.Combine(hostPluginDirectory, Path.GetFileName(modPackagePath));
        File.Copy(modPackagePath, target, overwrite: true);
        return target;
    }
}
