using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.Features.Archive.Views
{
  internal static class ArchiveDialogStyling
  {
    public static Brush GetBrush(FrameworkElement ownerElement, string key, Color fallbackColor)
    {
      if (ownerElement.TryFindResource(key) is Brush brush)
      {
        return brush;
      }

      if (Application.Current?.Resources[key] is Brush appBrush)
      {
        return appBrush;
      }

      return new SolidColorBrush(fallbackColor);
    }

    public static void TryApplyButtonStyle(FrameworkElement ownerElement, Button button)
    {
      if (ownerElement.TryFindResource("ButtonStyleV10") is Style style)
      {
        button.Style = style;
      }

      button.Height = 44;
      button.Padding = new Thickness(14, 6, 14, 6);
      button.FontSize = 16;
    }

    public static FontFamily? GetMediumFontFamily()
    {
      return Application.Current?.Resources["WinstonMedium"] as FontFamily;
    }
  }
}
