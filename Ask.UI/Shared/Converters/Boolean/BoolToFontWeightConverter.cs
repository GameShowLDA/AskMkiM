using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Boolean
{
  /// <summary>
  /// Преобразует логическое значение (<see cref="bool"/>) в соответствующее значение <see cref="FontWeight"/>.
  /// </summary>
  /// <remarks>
  /// Используется в привязках (Binding) для динамического изменения начертания текста в зависимости от состояния.
  /// <para>
  /// <see langword="true"/> → <see cref="FontWeights.Bold"/>.
  /// </para>
  /// <para>
  /// <see langword="false"/> → <see cref="FontWeights.Normal"/>.
  /// </para>
  /// </remarks>
  /// <example>
  /// Пример использования в XAML:
  /// <code>
  /// TextBlock FontWeight="{Binding IsHighlighted, Converter={StaticResource BoolToFontWeightConverter}}"/>
  /// </code>
  /// </example>
  /// <exception cref="InvalidCastException">
  /// Возникает, если входное значение не является типом <see cref="bool"/>.
  /// </exception>
  public class BoolToFontWeightConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует логическое значение в соответствующее значение <see cref="FontWeight"/>.
    /// </summary>
    /// <param name="value">
    /// Логическое значение, определяющее начертание шрифта.
    /// Ожидается тип <see cref="bool"/>.
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства привязки. Ожидается <see cref="FontWeight"/>.
    /// </param>
    /// <param name="parameter">Дополнительный параметр привязки (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// <see cref="FontWeights.Bold"/>, если значение равно <see langword="true"/>;  
    /// <see cref="FontWeights.Normal"/> — если <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Возникает, если <paramref name="value"/> не является типом <see cref="bool"/>.
    /// </exception>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is not bool flag)
        return FontWeights.Normal;

      return flag ? FontWeights.Bold : FontWeights.Normal;
    }

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <param name="value">Значение из целевого свойства.</param>
    /// <param name="targetType">Тип целевого свойства источника.</param>
    /// <param name="parameter">Дополнительный параметр привязки.</param>
    /// <param name="culture">Культура преобразования.</param>
    /// <returns>Не возвращает значение.</returns>
    /// <exception cref="NotSupportedException">
    /// Всегда генерируется, так как двустороннее преобразование не предусмотрено.
    /// </exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
  }
}
