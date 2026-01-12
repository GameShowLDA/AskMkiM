using Ask.Core.Services.Errors.Models;
using System.Globalization;
using System.Windows.Data;

namespace UI.Controls.ErrorList
{
  public class WarningLinePairConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is WarningItem warning)
      {
        if (warning.SourceLineNumber > 0 && warning.FormattedLineNumber > 0)
          return $"{warning.SourceLineNumber} ({warning.FormattedLineNumber})";
        else if (warning.SourceLineNumber > 0)
          return warning.SourceLineNumber.ToString();
        else if (warning.FormattedLineNumber > 0)
          return $"({warning.FormattedLineNumber})";
      }
      return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
  }
}
