namespace TheUnlocker.Modding;

public interface IAsyncModLifecycle
{
    Task OnPreLoadAsync(IModContext context, CancellationToken cancellationToken);

    Task OnLoadAsync(IModContext context, CancellationToken cancellationToken);

    Task OnAppReadyAsync(IModContext context, CancellationToken cancellationToken);

    Task OnUnloadAsync(CancellationToken cancellationToken);
}
