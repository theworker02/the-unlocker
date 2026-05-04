namespace TheUnlocker.Modding;

public interface ICommandPaletteService
{
    IReadOnlyCollection<string> Commands { get; }

    void Register(string commandName, Action execute);

    void Register(string commandName, string scope, Action execute);
}
