using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace UI.Windows.WpfDocking.Windows.Docking.Primitives
{
  public class DragMoveBehavior : Behavior<FrameworkElement>
  {
    protected override void OnAttached()
    {
      base.OnAttached();
      AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();
      AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 1 && e.LeftButton == MouseButtonState.Pressed)
      {
        var window = Window.GetWindow(AssociatedObject);
        try { window?.DragMove(); } catch { /* ignore if fails */ }
      }
    }
  }

}
