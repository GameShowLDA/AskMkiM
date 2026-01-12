using Ask.Core.Services.Config.AppSettings;
using System.Globalization;
using System.Windows.Data;

namespace UI.Components.ProtocolListBox
{
  internal sealed class HeaderVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      // value = Header
      var header = value as string;

      if (!ProtocolConfig.GetHeaderInfo())
        return string.Empty;

      if (string.IsNullOrWhiteSpace(header))
        return string.Empty;

      return header;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
  }
}
