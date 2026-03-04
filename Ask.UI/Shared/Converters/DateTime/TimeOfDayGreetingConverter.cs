using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.DateTime
{
  /// <summary>
  /// Формирует приветственное сообщение в зависимости от времени суток.
  /// </summary>
  /// <remarks>
  /// На основании часа из переданного <see cref="DateTime"/> 
  /// возвращает соответствующее приветствие:
  /// <list type="bullet">
  /// <item><description>05:00–10:59 — «Доброе утро.»</description></item>
  /// <item><description>11:00–16:59 — «Добрый день.»</description></item>
  /// <item><description>17:00–22:59 — «Добрый вечер.»</description></item>
  /// <item><description>23:00–04:59 — «Доброй ночи.»</description></item>
  /// </list>
  /// К приветствию добавляется предложение о запуске последней сессии.
  /// </remarks>
  public class TimeOfDayGreetingConverter : IValueConverter
  {
    private const string BaseMessage = " Запустить последнюю сессию?";

    /// <summary>
    /// Преобразует значение типа <see cref="DateTime"/> 
    /// в строку приветствия с учётом времени суток.
    /// </summary>
    /// <param name="value">
    /// Значение времени. Ожидается тип <see cref="DateTime"/>.
    /// </param>
    /// <param name="targetType">Тип целевого свойства (не используется).</param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Строка приветствия либо <see cref="string.Empty"/>, 
    /// если входное значение некорректно.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is not System.DateTime dateTime)
        return string.Empty;

      string greeting = GetGreetingByHour(dateTime.Hour);
      return BuildMessage(greeting);
    }

    /// <summary>
    /// Определяет текст приветствия по часу суток.
    /// </summary>
    /// <param name="hour">Час в диапазоне 0–23.</param>
    /// <returns>Строка приветствия.</returns>
    private static string GetGreetingByHour(int hour)
    {
      if (hour >= 5 && hour < 11)
        return "Доброе утро.";

      if (hour >= 11 && hour < 17)
        return "Добрый день.";

      if (hour >= 17 && hour < 23)
        return "Добрый вечер.";

      return "Доброй ночи.";
    }

    /// <summary>
    /// Формирует итоговое сообщение, объединяя приветствие 
    /// и базовую часть текста.
    /// </summary>
    /// <param name="greeting">Строка приветствия.</param>
    /// <returns>Полное сообщение для отображения.</returns>
    private static string BuildMessage(string greeting)
    {
      return greeting + BaseMessage;
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
