using System.Windows;

namespace UI.Windows.WpfDocking.Windows
{
  internal interface IWindow
  {
    Rect Bounds { get; }
    Rect ActualBounds { get; }
    double MinWidth { get; }
    double MaxWidth { get; }
    double MinHeight { get; }
    double MaxHeight { get; }
    void SetBounds(Rect bounds);
    void UpdateLayout();
  }
}
