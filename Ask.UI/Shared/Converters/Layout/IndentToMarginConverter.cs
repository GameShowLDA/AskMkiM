using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Layout
{
  /// <summary>
  /// Преобразует числовой уровень отступа в значение <see cref="Thickness"/>.
  /// </summary>
  /// <remarks>
  /// Каждый уровень отступа умножается на фиксированное значение 
  /// (<c>IndentSize</c>) и применяется к левому отступу.
  /// Верхний, правый и нижний отступы устанавливаются в 0.
  /// <para>
  /// Пример: при значении 2 и размере уровня 20.0 
  /// левый отступ будет равен 40.0.
  /// </para>
  /// </remarks>
  public class IndentToMarginConverter : IValueConverter
  {
    /// <summary>
    /// Размер одного уровня отступа в пикселях.
    /// </summary>
    private const double IndentSize = 20.0;

    /// <summary>
    /// Преобразует уровень вложенности в значение <see cref="Thickness"/>.
    /// </summary>
    /// <param name="value">
    /// Целочисленный уровень отступа (<see cref="int"/>).
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства привязки. Ожидается <see cref="Thickness"/>.
    /// </param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Объект <see cref="Thickness"/> с вычисленным левым отступом,
    /// либо нулевой отступ, если значение некорректно.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int indent)
        return new Thickness(indent * IndentSize, 0, 0, 0);

      return new Thickness(0);
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
