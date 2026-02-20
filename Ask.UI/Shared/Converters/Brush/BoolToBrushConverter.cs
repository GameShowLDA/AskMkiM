using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ask.UI.Shared.Converters.Brush
{
  /// <summary>
  /// Преобразует логическое значение (<see cref="bool"/>) 
  /// в соответствующее значение <see cref="Brush"/> 
  /// в зависимости от переданного параметра.
  /// </summary>
  /// <remarks>
  /// Поддерживаемые параметры:
  /// <list type="bullet">
  /// <item>
  /// <description>
  /// <c>"Highlight"</c> — возвращает синий цвет подсветки 
  /// (<see cref="Color.FromRgb(byte, byte, byte)"/> 0,120,215) при <see langword="true"/> 
  /// и <see cref="Brushes.Transparent"/> при <see langword="false"/>.
  /// </description>
  /// </item>
  /// <item>
  /// <description>
  /// <c>"Foreground"</c> — возвращает <see cref="Brushes.White"/> при <see langword="true"/> 
  /// и <see cref="Brushes.LightGray"/> при <see langword="false"/>.
  /// </description>
  /// </item>
  /// </list>
  /// При отсутствии параметра возвращается <see cref="Brushes.Transparent"/>.
  /// </remarks>
  public class BoolToBrushConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует логическое значение в <see cref="Brush"/> 
    /// в зависимости от <paramref name="parameter"/>.
    /// </summary>
    /// <param name="value">
    /// Логическое значение, определяющее выбор цвета. 
    /// Ожидается тип <see cref="bool"/>.
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства привязки. Ожидается <see cref="Brush"/>.
    /// </param>
    /// <param name="parameter">
    /// Строковый параметр, определяющий режим преобразования 
    /// (например, <c>"Highlight"</c> или <c>"Foreground"</c>).
    /// </param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Экземпляр <see cref="Brush"/> в зависимости от логического значения и параметра.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Возникает, если <paramref name="value"/> не является типом <see cref="bool"/>.
    /// </exception>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      bool val = (bool)value;

      if (parameter?.ToString() == "Highlight")
        return val ? new SolidColorBrush(Color.FromRgb(0, 120, 215)) : Brushes.Transparent;

      if (parameter?.ToString() == "Foreground")
        return val ? Brushes.White : Brushes.LightGray;

      return Brushes.Transparent;
    }

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Всегда генерируется, так как двустороннее преобразование не предусмотрено.
    /// </exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
  }

}
