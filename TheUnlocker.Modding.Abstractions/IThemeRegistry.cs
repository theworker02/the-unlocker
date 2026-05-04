namespace TheUnlocker.Modding;

public interface IThemeRegistry
{
    IReadOnlyCollection<string> Themes { get; }

    void Register(string themeName);
}
