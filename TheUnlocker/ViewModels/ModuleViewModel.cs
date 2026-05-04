using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheUnlocker.ContentManagement;

namespace TheUnlocker.ViewModels;

public sealed class ModuleViewModel : INotifyPropertyChanged
{
    private readonly Action<string, bool> _setEnabled;
    private bool _isEnabled;

    public ModuleViewModel(ModuleInfo module, Action<string, bool> setEnabled)
    {
        _setEnabled = setEnabled;
        Id = module.Id;
        Name = module.Name;
        Version = string.IsNullOrWhiteSpace(module.Version) ? "n/a" : module.Version;
        Path = module.Path;
        Status = module.Status.ToString();
        ErrorMessage = module.ErrorMessage ?? "";
        _isEnabled = module.Status == ModuleStatus.Active;
        CanInitialize = module.Status == ModuleStatus.Active;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string Name { get; }

    public string Version { get; }

    public string Path { get; }

    public string Status { get; }

    public string ErrorMessage { get; }

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

    public bool CanInitialize { get; }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
