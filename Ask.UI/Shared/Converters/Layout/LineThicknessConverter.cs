using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Layout
{
  /// <summary>
  /// Преобразует размер элемента в толщину линии,
  /// вычисляемую пропорционально переданному значению.
  /// </summary>
  /// <remarks>
  /// Толщина линии рассчитывается как 1/8 от переданного размера,
  /// но не менее 2 единиц.
  /// <para>
  /// Формула: <c>Max(2, size / 8)</c>.
  /// </para>
  /// Если входное значение некорректно, возвращается значение по умолчанию — 4.0.
  /// </remarks>
  public class LineThicknessConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует размер в толщину линии.
    /// </summary>
    /// <param name="value">
    /// Размер элемента (<see cref="double"/>).
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства привязки. Ожидается <see cref="double"/>.
    /// </param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Вычисленная толщина линии, не менее 2,
    /// либо 4.0 при некорректном входном значении.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is double size)
        return System.Math.Max(2, size / 8);

      return 4.0;
    }

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Всегда генерируется, так как двустороннее преобразование не предусмотрено.
    /// </exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
  }
}
