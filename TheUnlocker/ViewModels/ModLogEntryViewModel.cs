using TheUnlocker.Modding;

namespace TheUnlocker.ViewModels;

public sealed class ModLogEntryViewModel
{
    public ModLogEntryViewModel(ModLogEntry entry)
    {
        Timestamp = entry.Timestamp.ToString("u");
        ModId = entry.ModId;
        Severity = entry.Severity;
        EventType = entry.EventType;
        Message = entry.Message;
    }

    public string Timestamp { get; }
    public string ModId { get; }
    public string Severity { get; }
    public string EventType { get; }
    public string Message { get; }
}
