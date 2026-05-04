namespace TheUnlocker.Runtime;

public sealed class HookManager
{
    private readonly Dictionary<string, List<Action>> _hooks = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string hookName, Action callback)
    {
        if (!_hooks.TryGetValue(hookName, out var callbacks))
        {
            callbacks = new List<Action>();
            _hooks[hookName] = callbacks;
        }

        callbacks.Add(callback);
    }

    public void Invoke(string hookName)
    {
        if (!_hooks.TryGetValue(hookName, out var callbacks))
        {
            return;
        }

        foreach (var callback in callbacks.ToArray())
        {
            callback();
        }
    }
}
