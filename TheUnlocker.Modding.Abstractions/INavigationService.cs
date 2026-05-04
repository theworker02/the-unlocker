namespace TheUnlocker.Modding;

public interface INavigationService
{
    IReadOnlyCollection<string> Items { get; }

    void Register(string title);
}
