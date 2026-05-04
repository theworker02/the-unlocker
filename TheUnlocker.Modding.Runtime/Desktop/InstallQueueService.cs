using TheUnlocker.Modding;

namespace TheUnlocker.Desktop;

public sealed class InstallQueueService
{
    private readonly Queue<InstallQueueItem> _queue = new();
    private readonly List<InstallQueueItem> _history = new();

    public IReadOnlyCollection<InstallQueueItem> Pending => _queue.ToArray();
    public IReadOnlyCollection<InstallQueueItem> History => _history;

    public void Enqueue(string source)
    {
        _queue.Enqueue(new InstallQueueItem { Source = source });
    }

    public async Task ProcessAsync(ModInstaller installer, CancellationToken cancellationToken = default)
    {
        while (_queue.TryDequeue(out var item))
        {
            cancellationToken.ThrowIfCancellationRequested();
            item.Attempts++;
            item.Status = "Installing";
            try
            {
                await Task.Run(() => installer.Install(item.Source), cancellationToken);
                item.Status = "Installed";
            }
            catch (Exception ex)
            {
                item.Status = item.Attempts < 3 ? "RetryPending" : "Failed";
                item.LastError = ex.Message;
                if (item.Attempts < 3)
                {
                    _queue.Enqueue(item);
                    continue;
                }
            }

            _history.Add(item);
        }
    }
}
