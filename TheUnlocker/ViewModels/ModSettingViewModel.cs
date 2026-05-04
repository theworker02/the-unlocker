using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheUnlocker.Modding;

namespace TheUnlocker.ViewModels;

public sealed class ModSettingViewModel : INotifyPropertyChanged
{
    private readonly Action<string, string, string> _setValue;
    private string _value;

    public ModSettingViewModel(ModSettingInfo setting, Action<string, string, string> setValue)
    {
        _setValue = setValue;
        ModId = setting.ModId;
        Key = setting.Key;
        Label = setting.Label;
        Type = setting.Type;
        Options = setting.Options;
        _value = setting.Value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ModId { get; }

    public string Key { get; }

    public string Label { get; }

    public string Type { get; }

    public string[] Options { get; }

    public bool IsBoolean => Type.Equals("boolean", StringComparison.OrdinalIgnoreCase);

    public bool IsSelect => Type.Equals("select", StringComparison.OrdinalIgnoreCase);

    public bool UsesTextBox => !IsBoolean && !IsSelect;

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BooleanValue));
            _setValue(ModId, Key, value);
        }
    }

    public bool BooleanValue
    {
        get => bool.TryParse(Value, out var value) && value;
        set => Value = value.ToString();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
