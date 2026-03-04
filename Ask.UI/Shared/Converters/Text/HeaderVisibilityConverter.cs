using Ask.Core.Services.Config.AppSettings;
using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Text
{
  /// <summary>
  /// Возвращает заголовок, если его отображение разрешено конфигурацией
  /// и строка не является пустой.
  /// </summary>
  /// <remarks>
  /// Если в конфигурации отключено отображение заголовков
  /// либо переданное значение пустое или состоит только из пробелов,
  /// возвращается <see cref="string.Empty"/>.
  /// </remarks>
  internal sealed class HeaderVisibilityConverter : IValueConverter
  {
    /// <summary>
    /// Выполняет проверку возможности отображения заголовка.
    /// </summary>
    /// <param name="value">
    /// Строка заголовка (<see cref="string"/>).
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства (ожидается <see cref="string"/>).
    /// </param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Исходный заголовок либо <see cref="string.Empty"/>,
    /// если отображение запрещено или значение некорректно.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!ProtocolConfig.GetHeaderInfo())
        return string.Empty;

      if (value is not string header || string.IsNullOrWhiteSpace(header))
        return string.Empty;

      return header;
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
