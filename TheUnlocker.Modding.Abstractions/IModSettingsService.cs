namespace TheUnlocker.Modding;

public interface IModSettingsService
{
    string Get(string key, string fallback = "");

    void Set(string key, string value);
}
