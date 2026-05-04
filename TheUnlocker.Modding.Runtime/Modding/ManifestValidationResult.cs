namespace TheUnlocker.Modding;

public sealed class ManifestValidationResult
{
    public List<string> Errors { get; } = new();

    public List<string> Warnings { get; } = new();

    public bool IsValid => Errors.Count == 0;
}
