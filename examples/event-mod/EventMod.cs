using TheUnlocker.Modding;

public sealed class EventMod : IMod
{
    public string Id => "sample-event";
    public string Name => "Sample Event";
    public Version Version => new(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.Events.RegisterSchema("sample.loaded", new Version(1, 0, 0));
        context.Events.Publish(new SampleLoadedEvent(Id));
    }

    public void OnUnload()
    {
    }

    private sealed record SampleLoadedEvent(string ModId);
}
