namespace TheUnlocker.Modding;

public interface IModLifecycle
{
    void OnPreLoad(IModContext context);

    void OnAppReady(IModContext context);
}
