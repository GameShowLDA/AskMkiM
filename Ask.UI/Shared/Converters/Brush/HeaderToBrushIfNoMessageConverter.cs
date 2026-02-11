using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Protocol;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Ask.UI.Shared.Converters.Brush
{
  /// <summary>
  /// Определяет цвет заголовка в зависимости от наличия сообщения 
  /// и текущих настроек пользовательского интерфейса.
  /// </summary>
  /// <remarks>
  /// Ожидает три входных значения:
  /// <list type="number">
  /// <item><description>Заголовок (<see cref="string"/>).</description></item>
  /// <item><description>Сообщение (<see cref="string"/>).</description></item>
  /// <item><description>Текущий цвет заголовка (<see cref="SolidColorBrush"/>).</description></item>
  /// </list>
  /// <para>
  /// Если заголовок не пустой и сообщение отсутствует:
  /// </para>
  /// <list type="bullet">
  /// <item>
  /// <description>
  /// При включённой подсветке синтаксиса возвращается цвет успешного сообщения.
  /// </description>
  /// </item>
  /// <item>
  /// <description>
  /// Иначе используется ресурс <c>"TestsProtocolHeaderForeground"</c>
  /// из словаря ресурсов приложения.
  /// </description>
  /// </item>
  /// </list>
  /// <para>
  /// Во всех остальных случаях возвращается переданный цвет заголовка 
  /// либо белый цвет по умолчанию.
  /// </para>
  /// </remarks>
  public class HeaderToBrushIfNoMessageConverter : IMultiValueConverter
  {
    private static readonly SolidColorBrush SuccessBrush = new SolidColorBrush(ShowMessageModel.SuccessMessage.TitleColor);

    /// <summary>
    /// Выполняет преобразование набора входных значений в цвет заголовка.
    /// </summary>
    /// <param name="values">
    /// Массив входных значений:
    /// <list type="number">
    /// <item><description><see cref="string"/> — заголовок.</description></item>
    /// <item><description><see cref="string"/> — сообщение.</description></item>
    /// <item><description><see cref="SolidColorBrush"/> — текущий цвет заголовка.</description></item>
    /// </list>
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства привязки. Ожидается <see cref="Brush"/>.
    /// </param>
    /// <param name="parameter">Дополнительный параметр привязки (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Экземпляр <see cref="Brush"/> в зависимости от условий отображения.
    /// </returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      string header = values[0] as string;
      string message = values[1] as string;
      SolidColorBrush headerColor = values[2] as SolidColorBrush;

      if (!string.IsNullOrEmpty(header) && string.IsNullOrEmpty(message))
      {
        if (UserInterfaceConfig.GetSyntaxHighlighting())
        {
          return SuccessBrush;
        }

        return (SolidColorBrush)Application.Current.Resources["TestsProtocolHeaderForeground"];
      }

      return headerColor ?? new SolidColorBrush(Colors.White);
    }

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Всегда генерируется, так как двустороннее преобразование не предусмотрено.
    /// </exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
  }

}
