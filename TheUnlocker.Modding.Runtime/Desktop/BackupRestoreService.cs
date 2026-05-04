using System.IO.Compression;

namespace TheUnlocker.Desktop;

public sealed class BackupRestoreService
{
    public string Export(string contentRoot, string outputZipPath)
    {
        if (File.Exists(outputZipPath))
        {
            File.Delete(outputZipPath);
        }

        ZipFile.CreateFromDirectory(contentRoot, outputZipPath);
        return outputZipPath;
    }

    public void Restore(string backupZipPath, string contentRoot)
    {
        Directory.CreateDirectory(contentRoot);
        ZipFile.ExtractToDirectory(backupZipPath, contentRoot, overwriteFiles: true);
    }
}
