using System.Globalization;
using System.Windows.Data;
using static Ask.LogLib.LoggerUtility;

namespace UI.Localization
{
  /// <summary>
  /// Конвертер, возвращающий локализованную строку по ключу из ресурсов.
  /// Используется в XAML через ConverterParameter.
  /// </summary>
  public class LocalizationConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (parameter is string key)
      {
        var localizedValue = LocalizationService.Get(key);

        // Логирование в Output (Debug)
        LogInformation($"[LocalizationConverter] Culture: {CultureInfo.CurrentUICulture.Name}, Key: {key}, Value: {localizedValue}");

        return localizedValue;
      }

      return $"!{parameter}!";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
  }
}
