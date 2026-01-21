using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Ask.Core.Services.EventCore.Events.SystemStateEvents;

namespace UI.Windows.WpfDocking.Windows.Docking.Primitives
{
  /// <summary>Represents the tab in the document window.</summary>
  /// <remarks><see cref="DocumentTab" /> is the item container of <see cref="DocumentTabStrip"/>. Its <see cref="ContentControl.Content"/>
  /// property is set to a instance of <see cref="DockItem"/> object.</remarks>
  public class DocumentTab : DockWindowTab
  {
    /// <summary>Identifies the <see cref="P:UI.Windows.WpfDocking.Windows.Docking.Primitives.DocumentTab.ShowsIcon" /> attached property.</summary>
    /// <AttachedPropertyComments>
    /// <summary>Gets or sets the value that indicates whether the document tab icon should be displayed.</summary>
    /// <value><see langword="true"/> if the document tab icon should be displayed, otherwise <see langword="false"/>.</value>
    /// </AttachedPropertyComments>
    public static readonly DependencyProperty ShowsIconProperty = DependencyProperty.RegisterAttached(
        "ShowsIcon", typeof(bool), typeof(DocumentTab), new FrameworkPropertyMetadata(BooleanBoxes.False));

    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();
      EventAggregator.Subscribe<SystemStateEvents.LockedChanged>(OnLockedChanged);

      CloseButtonVisibility = Ask.Core.Services.Config.AppSettings.SystemStateManager.GetIsLocked() ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnLockedChanged(SystemStateEvents.LockedChanged e)
    {
      Dispatcher.Invoke(() =>
      {
        CloseButtonVisibility = e.IsLocked ? Visibility.Collapsed : Visibility.Visible;
      });
    }

    static DocumentTab()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(DocumentTab), new FrameworkPropertyMetadata(typeof(DocumentTab)));
    }

    public static readonly DependencyProperty CloseButtonVisibilityProperty =
        DependencyProperty.Register(
            nameof(CloseButtonVisibility),
            typeof(Visibility),
            typeof(DocumentTab),
            new FrameworkPropertyMetadata(Visibility.Visible));

    public Visibility CloseButtonVisibility
    {
      get => (Visibility)GetValue(CloseButtonVisibilityProperty);
      set => SetValue(CloseButtonVisibilityProperty, value);
    }

    /// <summary>Gets the value of <see cref="P:UI.Windows.WpfDocking.Windows.Docking.Primitives.DocumentTab.ShowsIcon" /> attached property
    /// from a given <see cref="DockControl" />.</summary>
    /// <param name="dockControl">The <see cref="DockControl"/> from which to read the property value.</param>
    /// <returns>The value of <see cref="P:UI.Windows.WpfDocking.Windows.Docking.Primitives.DocumentTab.ShowsIcon" /> attached property.</returns>
    public static bool GetShowsIcon(DockControl dockControl)
    {
      return (bool)dockControl.GetValue(ShowsIconProperty);
    }

    /// <summary>Sets the value of <see cref="P:UI.Windows.WpfDocking.Windows.Docking.Primitives.DocumentTab.ShowsIcon" /> attached property
    /// for a given <see cref="DockControl" />.</summary>
    /// <param name="dockControl">The <see cref="DockControl"/> on which to set the property value.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetShowsIcon(DockControl dockControl, bool value)
    {
      dockControl.SetValue(ShowsIconProperty, BooleanBoxes.Box(value));
    }

    /// <exclude />
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Middle)
      {
        CaptureMouse();
        e.Handled = true;
      }

      base.OnMouseDown(e);
    }

    /// <exclude />
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Middle && IsMouseCaptured)
      {
        ReleaseMouseCapture();
        DockItem.PerformClose();
        e.Handled = true;
      }

      base.OnMouseUp(e);
    }
  }
}
