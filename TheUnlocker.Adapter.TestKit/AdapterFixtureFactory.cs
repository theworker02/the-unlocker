using TheUnlocker.GameAdapters;

namespace TheUnlocker.Adapter.TestKit;

public sealed class AdapterFixtureFactory
{
    public string CreateUnityFixture(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Game_Data", "Managed"));
        File.WriteAllText(Path.Combine(root, "Game_Data", "Managed", "Assembly-CSharp.dll"), "");
        File.WriteAllText(Path.Combine(root, "Game_Data", "globalgamemanagers"), "unity");
        return root;
    }

    public string CreateUnrealFixture(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Binaries", "Win64"));
        File.WriteAllText(Path.Combine(root, "Binaries", "Win64", "Game-Win64-Shipping.exe"), "");
        Directory.CreateDirectory(Path.Combine(root, "Content", "Paks"));
        return root;
    }

    public string CreateMinecraftFixture(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".minecraft", "mods"));
        File.WriteAllText(Path.Combine(root, ".minecraft", "launcher_profiles.json"), "{}");
        return root;
    }

    public CompatibilityProbeResult AssertAdapterHandles(IGameAdapter adapter, string gameRoot)
    {
        if (!adapter.CanHandle(gameRoot))
        {
            return new CompatibilityProbeResult
            {
                AdapterId = adapter.Id,
                Passed = false,
                Errors = [$"{adapter.DisplayName} did not recognize {gameRoot}."]
            };
        }

        return new CompatibilityProbeResult
        {
            AdapterId = adapter.Id,
            Passed = true,
            Warnings = adapter.Inspect(gameRoot).Warnings
        };
    }
}
