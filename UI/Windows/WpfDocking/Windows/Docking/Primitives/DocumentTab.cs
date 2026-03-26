using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Windows.WpfDocking.Internal;
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

    /// <summary>
    /// Identifies the HideCloseButton attached property.
    /// </summary>
    public static readonly DependencyProperty HideCloseButtonProperty =
        DependencyProperty.RegisterAttached(
            "HideCloseButton",
            typeof(bool),
            typeof(DocumentTab),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.Inherits,
                OnHideCloseButtonChanged));

    public static bool GetHideCloseButton(DependencyObject obj)
    {
      return (bool)obj.GetValue(HideCloseButtonProperty);
    }

    public static void SetHideCloseButton(DependencyObject obj, bool value)
    {
      obj.SetValue(HideCloseButtonProperty, value);
    }

    private static void OnHideCloseButtonChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
      if (d is DocumentTab tab)
        tab.UpdateCloseButtonVisibility();
    }
    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();
      EventAggregator.Subscribe<SystemStateEvents.LockedChanged>(OnLockedChanged);
      UpdateCloseButtonVisibility();
    }

    private void OnLockedChanged(SystemStateEvents.LockedChanged e)
    {
      Dispatcher.Invoke(UpdateCloseButtonVisibility);
    }

    private void UpdateCloseButtonVisibility()
    {
      Debug.WriteLine(GetHideCloseButton(this));

      // 1. Явный запрет — имеет высший приоритет
      if (DockItem != null && GetHideCloseButton(DockItem))
      {
        CloseButtonVisibility = Visibility.Collapsed;
        return;
      }

      // 2. Иначе — зависит от SystemState
      bool isLocked = Ask.Core.Services.Config.AppSettings
          .SystemStateManager
          .GetIsLocked();

      CloseButtonVisibility = isLocked
          ? Visibility.Collapsed
          : Visibility.Visible;
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

        if (DockItem != null && GetHideCloseButton(DockItem))
        {
          e.Handled = true;
          return;
        }

        DockItem?.PerformClose();
        e.Handled = true;
      }

      base.OnMouseUp(e);
    }
  }
}
