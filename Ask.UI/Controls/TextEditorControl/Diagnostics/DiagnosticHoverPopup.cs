using Ask.Engine.ControlCommandAnalyser;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Ask.UI.Controls.TextEditorControl.Diagnostics;

/// <summary>Diagnostic card independent of the application's shortcut ToolTip template.</summary>
internal sealed class DiagnosticHoverPopup
{
  private readonly FrameworkElement _owner;
  private readonly Border _border;
  private readonly ListBox _messages;
  private readonly Popup _popup;
  private readonly DispatcherTimer _closeTimer;
  private IReadOnlyList<SourceDiagnostic> _diagnostics = Array.Empty<SourceDiagnostic>();

  internal DiagnosticHoverPopup(FrameworkElement owner)
  {
    _owner = owner;
    var resources = new ResourceDictionary
    {
      Source = new Uri("/Ask.UI;component/Controls/TextEditorControl/Diagnostics/DiagnosticHoverResources.xaml", UriKind.Relative),
    };
    _messages = new ListBox
    {
      Width = 500,
      MaxHeight = 320,
      Background = Brushes.Transparent,
      BorderThickness = new Thickness(0),
      Padding = new Thickness(0),
      Focusable = false,
      ItemTemplate = (DataTemplate)resources["DiagnosticMessageTemplate"],
      ItemContainerStyle = new Style(typeof(ListBoxItem))
      {
        Setters =
        {
          new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch),
          new Setter(Control.PaddingProperty, new Thickness(0)),
          new Setter(UIElement.FocusableProperty, false),
        },
      },
    };
    ScrollViewer.SetCanContentScroll(_messages, true);
    ScrollViewer.SetHorizontalScrollBarVisibility(_messages, ScrollBarVisibility.Disabled);
    ScrollViewer.SetVerticalScrollBarVisibility(_messages, ScrollBarVisibility.Auto);
    VirtualizingPanel.SetIsVirtualizing(_messages, true);
    VirtualizingPanel.SetVirtualizationMode(_messages, VirtualizationMode.Recycling);
    VirtualizingPanel.SetScrollUnit(_messages, ScrollUnit.Pixel);
    TextElement.SetFontFamily(_messages, SystemFonts.MessageFontFamily);
    TextElement.SetFontSize(_messages, 13);
    _border = new Border
    {
      Padding = new Thickness(12, 10, 14, 10),
      BorderThickness = new Thickness(1),
      CornerRadius = new CornerRadius(6),
      MaxWidth = 560,
      Child = _messages,
    };
    _popup = new Popup
    {
      PlacementTarget = owner,
      Placement = PlacementMode.Bottom,
      VerticalOffset = 5,
      AllowsTransparency = true,
      StaysOpen = true,
      Focusable = false,
      Child = _border,
    };
    _closeTimer = new DispatcherTimer(DispatcherPriority.Background, owner.Dispatcher)
    {
      Interval = TimeSpan.FromMilliseconds(180),
    };
    _closeTimer.Tick += (_, _) =>
    {
      _closeTimer.Stop();
      if (!_border.IsMouseOver) Close();
    };
    _border.MouseEnter += (_, _) => _closeTimer.Stop();
    _border.MouseLeave += (_, _) => ScheduleClose();
  }

  internal bool IsOpen => _popup.IsOpen;
  internal FrameworkElement Content => _border;

  internal void Show(IReadOnlyList<SourceDiagnostic> diagnostics, Rect anchor)
  {
    _closeTimer.Stop();
    if (diagnostics.Count == 0) { Close(); return; }
    if (!_popup.IsOpen || !ReferenceEquals(_diagnostics, diagnostics))
    {
      _diagnostics = diagnostics;
      _border.Background = GetBrush("ToolTipBackgroundBrush", Brushes.WhiteSmoke);
      _border.BorderBrush = GetBrush("ToolTipBorderBrush", Brushes.SlateGray);
      _messages.Foreground = GetBrush("ToolTipForegroundBrush", Brushes.Black);
      _messages.ItemsSource = diagnostics.DistinctBy(d => (d.IsWarning, d.Description)).ToArray();
      if (_messages.Items.Count > 0) _messages.ScrollIntoView(_messages.Items[0]);
    }
    _popup.PlacementRectangle = anchor;
    _popup.IsOpen = true;
  }

  internal void ScheduleClose()
  {
    if (!_popup.IsOpen || _closeTimer.IsEnabled) return;
    _closeTimer.Start();
  }

  internal void Close()
  {
    _closeTimer.Stop();
    _popup.IsOpen = false;
    _diagnostics = Array.Empty<SourceDiagnostic>();
    _messages.ItemsSource = null;
  }

  private Brush GetBrush(string key, Brush fallback) => _owner.TryFindResource(key) as Brush ?? fallback;
}
