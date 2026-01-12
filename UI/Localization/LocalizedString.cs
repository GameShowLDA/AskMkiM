using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using static Ask.LogLib.LoggerUtility;

namespace UI.Localization
{
  /// <summary>
  /// Представляет локализованную строку, автоматически обновляемую при смене языка.
  /// </summary>
  public class LocalizedString : INotifyPropertyChanged
  {
    private string _key = "";
    private static readonly List<LocalizedString> _instances = new();

    public LocalizedString()
    {
      _instances.Add(this);
    }

    public LocalizedString(string key) : this()
    {
      Key = key;
    }


    public string Key
    {
      get => _key;
      set
      {
        _key = value;
        OnPropertyChanged(nameof(Value)); // обновить Value при установке Key
      }
    }

    public string Value
    {
      get
      {
        var val = LocalizationService.Get(_key);
        LogInformation(
          $"[LocalizedString] Culture: {CultureInfo.CurrentUICulture.Name}, Key: {_key}, Value: {val}");
        return val;
      }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    public static void RefreshAll()
    {
      foreach (var instance in _instances)
      {
        instance.OnPropertyChanged(nameof(Value));
      }
    }
  }
}
