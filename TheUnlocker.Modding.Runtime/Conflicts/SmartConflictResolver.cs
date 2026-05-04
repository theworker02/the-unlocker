using TheUnlocker.Modding;

namespace TheUnlocker.Conflicts;

public enum ConflictResolutionAction
{
    ContinueWithWarning,
    DisableFirstMod,
    DisableSecondMod,
    ReorderLoadOrder,
    UseCompatibilityPatch
}

public sealed class ConflictResolutionSuggestion
{
    public ModConflictInfo Conflict { get; init; } = new() { Target = "", ModIds = "" };
    public ConflictResolutionAction Action { get; init; }
    public string Reason { get; init; } = "";
    public string? PatchModId { get; init; }
}

public sealed class CompatibilityPatch
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string[] BridgesMods { get; init; } = [];
    public string Description { get; init; } = "";
}

public sealed class SmartConflictResolver
{
    public IReadOnlyCollection<ConflictResolutionSuggestion> Suggest(IEnumerable<ModConflictInfo> conflicts, IEnumerable<CompatibilityPatch> patches)
    {
        var patchList = patches.ToList();
        return conflicts.Select(conflict =>
        {
            var conflictModIds = conflict.ModIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var patch = patchList.FirstOrDefault(x =>
                conflictModIds.All(id => x.BridgesMods.Contains(id, StringComparer.OrdinalIgnoreCase)));

            if (patch is not null)
            {
                return new ConflictResolutionSuggestion
                {
                    Conflict = conflict,
                    Action = ConflictResolutionAction.UseCompatibilityPatch,
                    PatchModId = patch.Id,
                    Reason = $"Compatibility patch '{patch.Name}' bridges this conflict."
                };
            }

            return new ConflictResolutionSuggestion
            {
                Conflict = conflict,
                Action = ConflictResolutionAction.ContinueWithWarning,
                Reason = "No bridge mod is installed; keep enabled only after testing."
            };
        }).ToList();
    }
}
