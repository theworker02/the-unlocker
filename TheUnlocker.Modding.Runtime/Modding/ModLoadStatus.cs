namespace TheUnlocker.Modding;

public enum ModLoadStatus
{
    Disabled,
    Loaded,
    Skipped,
    Error,
    MissingDependency,
    Incompatible
}
