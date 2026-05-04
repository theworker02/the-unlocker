namespace TheUnlocker.Modding;

public interface INotificationService
{
    IReadOnlyCollection<string> Notifications { get; }

    void Show(string message);
}
