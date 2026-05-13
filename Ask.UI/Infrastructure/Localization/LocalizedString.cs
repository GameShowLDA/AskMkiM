using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Ask.UI.Infrastructure.Localization
{
  /// <summary>
  /// Представляет биндинг-адаптер для получения локализованного текста
  /// по ключу ресурсов.
  /// 
  /// Используется в представлении для динамического обновления строк
  /// при изменении текущей культуры приложения. При смене языка
  /// вызывает уведомление об изменении свойства <see cref="Value"/>
  /// для всех созданных экземпляров.
  /// </summary>
  public class LocalizedString : INotifyPropertyChanged
  {
    private string _key = "";
    private static readonly List<LocalizedString> _instances = new();

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="LocalizedString"/>
    /// и регистрирует его для последующего обновления при смене языка.
    /// </summary>
    public LocalizedString()
    {
      _instances.Add(this);
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="LocalizedString"/>
    /// с указанным ключом локализации.
    /// </summary>
    /// <param name="key">Ключ строки в ресурсах.</param>
    public LocalizedString(string key) : this()
    {
      Key = key;
    }

    /// <summary>
    /// Ключ строки в ресурсах локализации.
    /// При изменении ключа автоматически инициирует обновление
    /// свойства <see cref="Value"/>.
    /// </summary>
    public string Key
    {
      get => _key;
      set
      {
        _key = value;
        OnPropertyChanged(nameof(Value));
      }
    }

    public string Value
    {
      get
      {
        var val = LocalizationService.Get(_key);
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
      void Refresh()
      {
        foreach (var instance in _instances)
        {
          instance.OnPropertyChanged(nameof(Value));
        }
      }

      var dispatcher = Application.Current?.Dispatcher;
      if (dispatcher != null && !dispatcher.CheckAccess())
      {
        dispatcher.Invoke(Refresh);
      }
      else
      {
        Refresh();
      }
    }
  }
}
