namespace TheUnlocker.GameDetection;

public sealed class UnityDetector
{
    public bool IsUnityGame(string gameDirectory)
    {
        return Directory.EnumerateFiles(gameDirectory, "Assembly-CSharp.dll", SearchOption.AllDirectories).Any()
            || Directory.EnumerateFiles(gameDirectory, "UnityPlayer.dll", SearchOption.TopDirectoryOnly).Any();
    }
}
