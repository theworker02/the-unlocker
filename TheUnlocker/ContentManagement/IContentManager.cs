namespace TheUnlocker.ContentManagement;

public interface IContentManager
{
    IReadOnlyCollection<ModuleInfo> Modules { get; }

    void Refresh();

    bool IsModuleAvailable(string moduleId);

    bool IsModuleEnabled(string moduleId);

    void SetModuleEnabled(string moduleId, bool enabled);

    ModuleInfo? GetModule(string moduleId);
}
