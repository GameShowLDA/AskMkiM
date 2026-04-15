using System.Windows;
using System.Windows.Media;

namespace UI.Controls.Archive
{
  internal enum ArchiveNodeStatus
  {
    None,
    Success,
    Error,
  }

  internal static class ArchiveNodeStatusExtensions
  {
    public static Visibility ToVisibility(this ArchiveNodeStatus status)
      => status == ArchiveNodeStatus.None ? Visibility.Collapsed : Visibility.Visible;

    public static Brush ToBrush(this ArchiveNodeStatus status)
      => status switch
      {
        ArchiveNodeStatus.Success => new SolidColorBrush(Color.FromRgb(95, 197, 95)),
        ArchiveNodeStatus.Error => new SolidColorBrush(Color.FromRgb(224, 92, 92)),
        _ => Brushes.Transparent,
      };
  }
}
