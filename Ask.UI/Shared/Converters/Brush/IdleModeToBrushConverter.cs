using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Brush
{
  /// <summary>
  /// Преобразует логическое значение, указывающее на режим простоя,
  /// в соответствующий ресурс <see cref="Brush"/> из словаря приложения.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Если значение равно <see langword="true"/>, возвращается ресурс
  /// <c>"GreenColorSolidColorBrush"</c>.
  /// </para>
  /// <para>
  /// В противном случае возвращается ресурс
  /// <c>"ActiveBorderSolidColorBrush"</c>.
  /// </para>
  /// <para>
  /// Использует <see cref="Application.Current"/> для получения ресурсов.
  /// </para>
  /// </remarks>
  public class IdleModeToBrushConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует логическое значение в кисть из ресурсов приложения.
    /// </summary>
    /// <param name="value">
    /// Логическое значение, определяющее режим простоя.
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства привязки. Ожидается <see cref="Brush"/>.
    /// </param>
    /// <param name="parameter">Дополнительный параметр привязки (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Ресурс <see cref="Brush"/> в зависимости от состояния режима.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is bool isIdle && isIdle)
        return Application.Current.Resources["GreenColorSolidColorBrush"];

      return Application.Current.Resources["ActiveBorderSolidColorBrush"];
    }

    /// <summary>
    /// Обратное преобразование не выполняется.
    /// </summary>
    /// <remarks>
    /// Возвращает <see cref="Binding.DoNothing"/>, чтобы предотвратить
    /// обновление источника привязки.
    /// </remarks>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
  }

}
