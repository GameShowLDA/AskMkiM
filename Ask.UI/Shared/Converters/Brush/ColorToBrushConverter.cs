using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ask.UI.Shared.Converters.Brush
{
  /// <summary>
  /// Преобразует значение типа <see cref="Color"/> 
  /// в соответствующий экземпляр <see cref="SolidColorBrush"/>.
  /// </summary>
  /// <remarks>
  /// Используется в привязках для преобразования цвета модели 
  /// в объект <see cref="Brush"/>, необходимый для UI-свойств 
  /// (например, <c>Background</c> или <c>Foreground</c>).
  /// <para>
  /// Если входное значение не является типом <see cref="Color"/>, 
  /// возвращается <see cref="Brushes.Transparent"/>.
  /// </para>
  /// </remarks>
  public class ColorToBrushConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует <see cref="Color"/> в <see cref="SolidColorBrush"/>.
    /// </summary>
    /// <param name="value">
    /// Значение цвета. Ожидается тип <see cref="Color"/>.
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства привязки. Ожидается <see cref="Brush"/>.
    /// </param>
    /// <param name="parameter">Дополнительный параметр привязки (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Экземпляр <see cref="SolidColorBrush"/>, если значение является <see cref="Color"/>;  
    /// иначе <see cref="Brushes.Transparent"/>.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return value is Color color
          ? new SolidColorBrush(color)
          : Brushes.Transparent;
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
