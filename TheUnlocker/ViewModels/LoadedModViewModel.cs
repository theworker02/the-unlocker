using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheUnlocker.Modding;

namespace TheUnlocker.ViewModels;

public sealed class LoadedModViewModel : INotifyPropertyChanged
{
    private readonly Action<string, bool> _setEnabled;
    private bool _isEnabled;

    public LoadedModViewModel(LoadedModInfo mod, Action<string, bool> setEnabled)
    {
        _setEnabled = setEnabled;
        Id = mod.Id;
        Name = mod.Name;
        Version = mod.Version;
        Author = mod.Author;
        Description = mod.Description;
        AssemblyPath = mod.AssemblyPath;
        Status = mod.Status.ToString();
        SignatureStatus = mod.SignatureStatus.ToString();
        Dependencies = mod.Dependencies.Length == 0 ? "none" : string.Join(", ", mod.Dependencies);
        Permissions = mod.Permissions.Length == 0 ? "none" : string.Join(", ", mod.Permissions);
        Settings = mod.Settings.Count == 0 ? "none" : string.Join(", ", mod.Settings.Keys);
        Message = mod.Message ?? "";
        _isEnabled = mod.IsEnabled;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string Name { get; }

    public string Version { get; }

    public string Author { get; }

    public string Description { get; }

    public string AssemblyPath { get; }

    public string Status { get; }

    public string SignatureStatus { get; }

    public string Dependencies { get; }

    public string Permissions { get; }

    public string Settings { get; }

    public string Message { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
            {
                return;
            }

            _isEnabled = value;
            OnPropertyChanged();
            _setEnabled(Id, value);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
