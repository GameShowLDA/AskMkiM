using System.Windows;
using System.Windows.Media;

namespace Ask.UI.Features.Archive.Services
{
  public static class ArchiveDialogHostService
  {
    public static Window CreateDialogWindow(FrameworkElement ownerElement, string title)
    {
      ArgumentNullException.ThrowIfNull(ownerElement);

      return new Window
      {
        Title = title,
        Owner = Window.GetWindow(ownerElement),
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize,
        SizeToContent = SizeToContent.WidthAndHeight,
        ShowInTaskbar = false,
        WindowStyle = WindowStyle.None,
        AllowsTransparency = true,
        Background = Brushes.Transparent,
      };
    }
  }
}
