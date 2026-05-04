namespace TheUnlocker.AI;

public sealed class ModGenerator
{
    public string GenerateSafePromptPlan(string request)
    {
        return $"""
Safe mod generation plan:
1. Describe the intended gameplay or UI extension.
2. Use official TheUnlocker extension points only.
3. Declare permissions and affected systems in mod.json.
4. Generate IMod code without bypassing ownership, integrity, anti-cheat, or protected checks.

Request:
{request}
""";
    }
}
