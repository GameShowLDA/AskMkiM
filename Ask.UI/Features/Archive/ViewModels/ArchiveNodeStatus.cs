using System.Windows;
using System.Windows.Media;

namespace Ask.UI.Features.Archive.ViewModels
{
  /// <summary>
  /// Состояние узла архива.
  /// </summary>
  public enum ArchiveNodeStatus
  {
    /// <summary>
    /// Состояние не задано.
    /// </summary>
    None,

    /// <summary>
    /// Операция выполнена успешно.
    /// </summary>
    Success,

    /// <summary>
    /// Обнаружены ошибки.
    /// </summary>
    Error,
  }

  /// <summary>
  /// Методы расширения для преобразования состояния узла архива в UI-представление.
  /// </summary>
  internal static class ArchiveNodeStatusExtensions
  {
    /// <summary>
    /// Преобразует состояние узла архива в значение видимости.
    /// </summary>
    /// <param name="status">Состояние узла архива.</param>
    /// <returns>
    /// Visibility.Visible для состояний Success и Error;
    /// иначе Visibility.Collapsed.
    /// </returns>
    public static Visibility ToVisibility(this ArchiveNodeStatus status)
      => status == ArchiveNodeStatus.None ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Преобразует состояние узла архива в цветовую кисть.
    /// </summary>
    /// <param name="status">Состояние узла архива.</param>
    /// <returns>Кисть, соответствующая состоянию узла.</returns>
    public static Brush ToBrush(this ArchiveNodeStatus status)
      => status switch
      {
        ArchiveNodeStatus.Success => new SolidColorBrush(Color.FromRgb(95, 197, 95)),
        ArchiveNodeStatus.Error => new SolidColorBrush(Color.FromRgb(224, 92, 92)),
        _ => Brushes.Transparent,
      };
  }
}
