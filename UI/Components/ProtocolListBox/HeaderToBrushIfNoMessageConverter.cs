using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Protocol;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UI.Components.ProtocolListBox
{
  public class HeaderToBrushIfNoMessageConverter : IMultiValueConverter
  {
    private static readonly SolidColorBrush SuccessBrush = new SolidColorBrush(ShowMessageModel.SuccessMessage.TitleColor);

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      string header = values[0] as string;
      string message = values[1] as string;
      SolidColorBrush headerColor = values[2] as SolidColorBrush;

      if (!string.IsNullOrEmpty(header) && string.IsNullOrEmpty(message))
      {
        if (UserInterfaceConfig.GetSyntaxHighlighting())
        {
          return headerColor ?? SuccessBrush;
        }

        return (SolidColorBrush)Application.Current.Resources["TestsProtocolHeaderForeground"];
      }
      return headerColor ?? new SolidColorBrush(Colors.White);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
