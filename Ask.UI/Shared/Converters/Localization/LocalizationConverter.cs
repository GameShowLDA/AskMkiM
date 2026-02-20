using Ask.UI.Infrastructure.Localization;
using System.Globalization;
using System.Windows.Data;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Shared.Converters.Localization
{
  /// <summary>
  /// Возвращает локализованную строку по ключу,
  /// переданному через <see cref="IValueConverter"/>.
  /// </summary>
  /// <remarks>
  /// Ключ локализации передаётся через <paramref name="parameter"/>.
  /// Значение <paramref name="value"/> не используется.
  /// 
  /// Пример использования в XAML:
  /// <code>
  /// Text="{Binding Converter={StaticResource LocalizationConverter},
  ///                ConverterParameter=StartButtonText}"
  /// </code>
  /// 
  /// Если ключ отсутствует или не является строкой,
  /// возвращается маркер вида <c>!Key!</c>.
  /// </remarks>
  public sealed class LocalizationConverter : IValueConverter
  {
    /// <summary>
    /// Возвращает локализованную строку по указанному ключу.
    /// </summary>
    /// <param name="value">
    /// Значение источника привязки (не используется).
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства (ожидается <see cref="string"/>).
    /// </param>
    /// <param name="parameter">
    /// Ключ локализации (<see cref="string"/>).
    /// </param>
    /// <param name="culture">
    /// Культура преобразования (не используется, 
    /// используется <see cref="CultureInfo.CurrentUICulture"/>).
    /// </param>
    /// <returns>
    /// Локализованная строка либо маркер <c>!Key!</c>,
    /// если ключ невалиден.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (parameter is not string key || string.IsNullOrWhiteSpace(key))
        return "!InvalidLocalizationKey!";

      string localizedValue = LocalizationService.Get(key) ?? $"!{key}!";
      LogInformation($"[LocalizationConverter] Culture: {CultureInfo.CurrentUICulture.Name}, Key: {key}, Value: {localizedValue}");

      return localizedValue;
    }

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Всегда генерируется, так как двусторонняя привязка не предусмотрена.
    /// </exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
  }

}
