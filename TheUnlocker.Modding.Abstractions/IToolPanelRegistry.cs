namespace TheUnlocker.Modding;

public interface IToolPanelRegistry
{
    IReadOnlyCollection<string> Panels { get; }

    void Register(string panelName);
}
