namespace TheUnlocker.Modding;

public sealed class ModEventBus : IModEventBus
{
    private readonly IReadOnlySet<string> _permissions;
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly List<string> _schemas = new();
    private readonly Action? _onHandlerRegistered;

    public ModEventBus(IReadOnlySet<string> permissions, Action? onHandlerRegistered = null)
    {
        _permissions = permissions;
        _onHandlerRegistered = onHandlerRegistered;
    }

    public IReadOnlyCollection<string> RegisteredSchemas => _schemas;

    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        Demand();
        var eventType = typeof(TEvent);
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<Delegate>();
            _handlers[eventType] = handlers;
        }

        handlers.Add(handler);
        _onHandlerRegistered?.Invoke();
    }

    public void RegisterSchema(string eventName, Version version)
    {
        Demand();
        _schemas.Add($"{eventName}@{version}");
    }

    public void Publish<TEvent>(TEvent eventData)
    {
        Demand();
        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.Cast<Action<TEvent>>().ToArray())
        {
            handler(eventData);
        }
    }

    private void Demand()
    {
        if (!_permissions.Contains(ModPermission.Events))
        {
            throw new UnauthorizedAccessException($"Mod does not declare required permission: {ModPermission.Events}");
        }
    }
}
