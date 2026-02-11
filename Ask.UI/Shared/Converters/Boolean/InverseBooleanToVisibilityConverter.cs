using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Boolean
{
  /// <summary>
  /// Преобразует логическое значение (<see cref="bool"/>) 
  /// в инвертированное значение <see cref="Visibility"/>.
  /// </summary>
  /// <remarks>
  /// Используется в привязках для скрытия элемента при истинном значении источника.
  /// <para>
  /// <see langword="true"/> → <see cref="Visibility.Collapsed"/>.
  /// </para>
  /// <para>
  /// <see langword="false"/> → <see cref="Visibility.Visible"/>.
  /// </para>
  /// </remarks>
  public class InverseBooleanToVisibilityConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует логическое значение в инвертированное значение <see cref="Visibility"/>.
    /// </summary>
    /// <param name="value">
    /// Логическое значение, определяющее видимость элемента.
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства привязки. Ожидается <see cref="Visibility"/>.
    /// </param>
    /// <param name="parameter">Дополнительный параметр привязки (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// <see cref="Visibility.Collapsed"/>, если значение равно <see langword="true"/>;  
    /// <see cref="Visibility.Visible"/> — в остальных случаях.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      bool b = value is bool boolVal && boolVal;
      return b ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Выполняет обратное преобразование значения <see cref="Visibility"/> 
    /// в логическое значение с инверсией.
    /// </summary>
    /// <param name="value">
    /// Значение видимости элемента.
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства источника. Ожидается <see cref="bool"/>.
    /// </param>
    /// <param name="parameter">Дополнительный параметр привязки (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// <see langword="true"/>, если значение не равно <see cref="Visibility.Visible"/>;  
    /// <see langword="false"/> — если значение равно <see cref="Visibility.Visible"/>.
    /// </returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (value is Visibility visibility) && visibility != Visibility.Visible;
    }
  }
}
