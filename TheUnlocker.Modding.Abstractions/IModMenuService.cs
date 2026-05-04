namespace TheUnlocker.Modding;

public interface IModMenuService
{
    IReadOnlyCollection<ModMenuItem> Items { get; }

    void Add(string title, Action execute);
}
