using TheUnlocker.Modding;

namespace SampleMod;

public sealed class HelloWorldMod : IMod, IModLifecycle, IAsyncModLifecycle
{
    public string Id => "hello-world";

    public string Name => "Hello World Mod";

    public Version Version => new(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.Log(Id, "Loaded and ready.");

        var greeting = context.Settings.Get("greeting", "Hello from the sample mod.");
        var showNotification = bool.TryParse(context.Settings.Get("showNotification", "true"), out var enabled) && enabled;
        if (showNotification)
        {
            context.Notifications.Show(greeting);
        }

        context.AssetRegistry.Register("assets/sample.txt");
        context.MenuItems.Add("Sample Command", () => context.Log(Id, "Sample command executed."));
        context.Navigation.Register("Sample Mod");
        context.AssetImporters.Register(".sample", "Sample Asset");
        context.Themes.Register("Sample Accent");
        context.CommandPalette.Register("Sample Command", () => context.Log(Id, "Command palette action executed."));
        context.CommandPalette.Register("Scoped Sample Command", "local", () => context.Log(Id, "Scoped command executed."));
        context.ToolPanels.Register("Sample Tool Panel");
        context.Events.Subscribe<string>(message => context.Log(Id, $"Received event: {message}"));
        context.Events.Publish("Sample mod startup event.");
    }

    public void OnUnload()
    {
    }

    public void OnPreLoad(IModContext context)
    {
        context.Log(Id, "Pre-load phase.");
    }

    public void OnAppReady(IModContext context)
    {
        context.Log(Id, "App-ready phase.");
    }

    public Task OnPreLoadAsync(IModContext context, CancellationToken cancellationToken)
    {
        context.Log(Id, "Async pre-load phase.");
        return Task.CompletedTask;
    }

    public Task OnLoadAsync(IModContext context, CancellationToken cancellationToken)
    {
        context.Log(Id, "Async load phase.");
        return Task.CompletedTask;
    }

    public Task OnAppReadyAsync(IModContext context, CancellationToken cancellationToken)
    {
        context.Log(Id, "Async app-ready phase.");
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
