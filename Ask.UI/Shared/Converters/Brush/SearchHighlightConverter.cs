using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ask.UI.Shared.Converters.Brush
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
      var (fullText, searchText) = ExtractInput(values);

      if (ShouldReturnPlainText(fullText, searchText))
        return CreatePlainText(fullText);

      return BuildHighlightedInlines(fullText, searchText);
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

    /// <summary>
    /// Извлекает исходный и искомый текст из массива значений конвертера.
    /// При некорректных входных данных возвращает пустые строки.
    /// </summary>
    /// <param name="values">
    /// Массив значений, где:
    /// [0] — полный текст,
    /// [1] — текст для поиска.
    /// </param>
    /// <returns>
    /// Кортеж, содержащий полный текст и искомую строку.
    /// </returns>
    private static (string fullText, string searchText) ExtractInput(object[] values)
    {
      if (values == null || values.Length < 2)
        return (string.Empty, string.Empty);

      return (values[0] as string ?? string.Empty,
              values[1] as string ?? string.Empty);
    }

    /// <summary>
    /// Определяет, требуется ли выполнять подсветку текста.
    /// </summary>
    /// <param name="fullText">Полный текст.</param>
    /// <param name="searchText">Текст для поиска.</param>
    /// <returns>
    /// <c>true</c>, если подсветка не требуется (один из параметров пустой);
    /// иначе <c>false</c>.
    /// </returns>
    private static bool ShouldReturnPlainText(string fullText, string searchText)
    {
      return string.IsNullOrEmpty(fullText) || string.IsNullOrEmpty(searchText);
    }

    /// <summary>
    /// Создает коллекцию <see cref="Inline"/>, содержащую один элемент
    /// без подсветки.
    /// </summary>
    /// <param name="text">Текст для отображения.</param>
    /// <returns>
    /// Список <see cref="Inline"/> с обычным текстом.
    /// </returns>
    private static List<Inline> CreatePlainText(string text)
    {
      return new List<Inline>
      {
        new Run(text ?? string.Empty)
      };
    }

    /// <summary>
    /// Формирует список <see cref="Inline"/> с выделением всех
    /// совпадений искомой строки в полном тексте.
    /// </summary>
    /// <param name="fullText">Полный текст.</param>
    /// <param name="searchText">Текст для поиска.</param>
    /// <returns>
    /// Список <see cref="Inline"/> с подсвеченными совпадениями.
    /// </returns>
    private static List<Inline> BuildHighlightedInlines(string fullText, string searchText)
    {
      var inlines = new List<Inline>();
      var regex = CreateSearchRegex(searchText);

      int lastIndex = 0;

      foreach (Match match in regex.Matches(fullText))
      {
        AddNonHighlightedPart(inlines, fullText, lastIndex, match.Index);
        AddHighlightedPart(inlines, fullText, match.Index, match.Length);

        lastIndex = match.Index + match.Length;
      }

      AddRemainingText(inlines, fullText, lastIndex);

      return inlines;
    }

    /// <summary>
    /// Создает объект <see cref="Regex"/> для поиска строки
    /// без учета регистра.
    /// </summary>
    /// <param name="searchText">Текст для поиска.</param>
    /// <returns>
    /// Экземпляр <see cref="Regex"/> с экранированным шаблоном.
    /// </returns>
    private static Regex CreateSearchRegex(string searchText)
    {
      return new Regex(
          Regex.Escape(searchText),
          RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Добавляет в коллекцию непрерывный фрагмент текста без подсветки.
    /// </summary>
    /// <param name="inlines">Коллекция для добавления элементов.</param>
    /// <param name="text">Исходный текст.</param>
    /// <param name="startIndex">Начальный индекс фрагмента.</param>
    /// <param name="endIndex">Конечный индекс фрагмента.</param>
    private static void AddNonHighlightedPart(ICollection<Inline> inlines, string text, int startIndex, int endIndex)
    {
      if (endIndex <= startIndex)
        return;

      inlines.Add(new Run(text.Substring(startIndex, endIndex - startIndex)));
    }

    /// <summary>
    /// Добавляет в коллекцию фрагмент текста с визуальным выделением.
    /// </summary>
    /// <param name="inlines">Коллекция для добавления элементов.</param>
    /// <param name="text">Исходный текст.</param>
    /// <param name="index">Индекс начала совпадения.</param>
    /// <param name="length">Длина совпадения.</param>
    private static void AddHighlightedPart(ICollection<Inline> inlines, string text, int index, int length)
    {
      var run = new Run(text.Substring(index, length))
      {
        Foreground = Brushes.Red,
        FontWeight = FontWeights.Bold
      };

      inlines.Add(run);
    }
    /// <summary>
    /// Добавляет оставшуюся часть текста после последнего совпадения.
    /// </summary>
    /// <param name="inlines">Коллекция для добавления элементов.</param>
    /// <param name="text">Исходный текст.</param>
    /// <param name="lastIndex">Индекс, с которого начинается остаток текста.</param>
    private static void AddRemainingText(ICollection<Inline> inlines, string text, int lastIndex)
    {
      if (lastIndex < text.Length)
        inlines.Add(new Run(text.Substring(lastIndex)));
    }
  }
}
