namespace TheUnlocker.Modding;

public interface IModEventBus
{
    void Subscribe<TEvent>(Action<TEvent> handler);

    void Publish<TEvent>(TEvent eventData);

    IReadOnlyCollection<string> RegisteredSchemas { get; }

    void RegisterSchema(string eventName, Version version);
}
