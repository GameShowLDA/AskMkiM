using Ask.Core.Services.Errors.Models;
using System.Globalization;
using System.Windows.Data;

namespace UI.Controls.ErrorList
{
  public class DiagnosticsCodeLinePairConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is IDisplayIssue error)
      {
        if (error.SourceLineNumber > 0 && error.FormattedLineNumber > 0)
          return $"{error.SourceLineNumber} ({error.FormattedLineNumber})";
        else if (error.SourceLineNumber > 0)
          return error.SourceLineNumber.ToString();
        else if (error.FormattedLineNumber > 0)
          return $"({error.FormattedLineNumber})";
      }
      return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
  }
}
