using Ask.Core.Services.Errors.Models;
using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Controls.ErrorList
{
  public sealed class IssueDebugInfoConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return value is ErrorItem error
        ? error.DebugInfo
        : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      => throw new NotSupportedException();
  }
}
