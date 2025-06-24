using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Utilities.Models;

namespace UI.Controls.ErrorList
{
  public class ErrorLinePairConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is ErrorItem error)
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
