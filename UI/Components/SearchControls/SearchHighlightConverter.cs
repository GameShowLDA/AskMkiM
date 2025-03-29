using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Преобразователь, который выделяет все вхождения искомого текста в строке.
  /// </summary>
  /// <remarks>
  /// Этот класс реализует интерфейс <see cref="IMultiValueConverter"/> и используется для преобразования текста в список объектов <see cref="Inline"/> с выделением искомого текста.
  /// Преобразователь ищет все вхождения указанного поискового текста в полном тексте, игнорируя регистр, и выделяет их с помощью красного цвета и жирного шрифта.
  /// </remarks>
  public class SearchHighlightConverter : IMultiValueConverter
  {
    /// <summary>
    /// Преобразует массив значений в список объектов <see cref="Inline"/> для отображения в тексте с выделением.
    /// </summary>
    /// <param name="values">Массив значений, где:
    /// - [0] <see cref="string"/>: полный текст, в котором выполняется поиск.
    /// - [1] <see cref="string"/>: искомый текст, который нужно выделить.</param>
    /// <param name="targetType">Тип, в который выполняется преобразование (не используется).</param>
    /// <param name="parameter">Параметр (не используется).</param>
    /// <param name="culture">Культура (не используется).</param>
    /// <returns>Список объектов <see cref="Inline"/>, представляющих текст с выделенными совпадениями.</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      string fullText = values[0] as string;
      string searchText = values[1] as string;

      if (string.IsNullOrEmpty(fullText) || string.IsNullOrEmpty(searchText))
      {
        return new List<Inline> { new Run(fullText) };
      }

      List<Inline> inlines = new List<Inline>();
      Regex regex = new Regex(Regex.Escape(searchText), RegexOptions.IgnoreCase);
      int lastIndex = 0;
      foreach (Match match in regex.Matches(fullText))
      {
        if (match.Index > lastIndex)
        {
          inlines.Add(new Run(fullText.Substring(lastIndex, match.Index - lastIndex)));
        }

        Run highlightRun = new Run(fullText.Substring(match.Index, match.Length))
        {
          Foreground = Brushes.Red,
          FontWeight = FontWeights.Bold,
        };
        inlines.Add(highlightRun);
        lastIndex = match.Index + match.Length;
      }

      if (lastIndex < fullText.Length)
      {
        inlines.Add(new Run(fullText.Substring(lastIndex)));
      }

      return inlines;
    }

    /// <summary>
    /// Метод не реализован, так как двустороннее преобразование не требуется.
    /// </summary>
    /// <param name="value">Значение для преобразования назад.</param>
    /// <param name="targetTypes">Массив типов для преобразования.</param>
    /// <param name="parameter">Параметр (не используется).</param>
    /// <param name="culture">Культура (не используется).</param>
    /// <returns>Исключение <see cref="NotImplementedException"/>, так как преобразование назад не поддерживается.</returns>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
