using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  public class SearchHighlightConverter : IValueConverter
  {
    // Параметр конвертера – это текст для поиска
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      string fullText = value as string;
      string searchText = parameter as string;

      if (string.IsNullOrEmpty(fullText) || string.IsNullOrEmpty(searchText))
        return new List<Inline> { new Run(fullText) };

      var inlines = new List<Inline>();
      // Используем Regex, чтобы найти все совпадения (можно настроить флаги, если нужно)
      Regex regex = new Regex(Regex.Escape(searchText), RegexOptions.IgnoreCase);
      int lastIndex = 0;

      foreach (Match match in regex.Matches(fullText))
      {
        // Добавляем текст до совпадения обычным стилем
        if (match.Index > lastIndex)
        {
          inlines.Add(new Run(fullText.Substring(lastIndex, match.Index - lastIndex)));
        }
        // Добавляем найденное совпадение с другим цветом
        var highlightRun = new Run(fullText.Substring(match.Index, match.Length))
        {
          Foreground = Brushes.Red  // замените на нужный цвет
        };
        inlines.Add(highlightRun);
        lastIndex = match.Index + match.Length;
      }

      // Добавляем остаток строки
      if (lastIndex < fullText.Length)
      {
        inlines.Add(new Run(fullText.Substring(lastIndex)));
      }

      return inlines;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
