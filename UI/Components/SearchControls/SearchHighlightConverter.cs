using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  public class SearchHighlightConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      string fullText = values[0] as string;
      string searchText = values[1] as string;

      if (string.IsNullOrEmpty(fullText) || string.IsNullOrEmpty(searchText))
        return new List<Inline> { new Run(fullText) };

      List<Inline> inlines = new List<Inline>();
      Regex regex = new Regex(Regex.Escape(searchText), RegexOptions.IgnoreCase);
      int lastIndex = 0;
      foreach (Match match in regex.Matches(fullText))
      {
        if (match.Index > lastIndex)
          inlines.Add(new Run(fullText.Substring(lastIndex, match.Index - lastIndex)));
        Run highlightRun = new Run(fullText.Substring(match.Index, match.Length))
        {
          Foreground = Brushes.Red,
          FontWeight = FontWeights.Bold
        };
        inlines.Add(highlightRun);
        lastIndex = match.Index + match.Length;
      }
      if (lastIndex < fullText.Length)
        inlines.Add(new Run(fullText.Substring(lastIndex)));
      return inlines;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
