using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Boolean
{
  /// <summary>
  /// Преобразует логическое значение (<see cref="bool"/>) 
  /// в значение прозрачности (<see cref="double"/>) для UI-элемента.
  /// </summary>
  /// <remarks>
  /// Используется для визуального выделения или приглушения элементов интерфейса.
  /// <para>
  /// <see langword="true"/> → 1.0 (полная непрозрачность).
  /// </para>
  /// <para>
  /// <see langword="false"/> → 0.5 (полупрозрачность).
  /// </para>
  /// </remarks>
  public class BoolToOpacityConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует логическое значение в коэффициент прозрачности.
    /// </summary>
    /// <param name="value">
    /// Логическое значение, определяющее уровень прозрачности.
    /// Ожидается тип <see cref="bool"/>.
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства привязки. Ожидается <see cref="double"/>.
    /// </param>
    /// <param name="parameter">Дополнительный параметр привязки (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// 1.0, если значение равно <see langword="true"/>;  
    /// 0.5 — если <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Возникает, если <paramref name="value"/> не является типом <see cref="bool"/>.
    /// </exception>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? 1.0 : 0.5;

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
