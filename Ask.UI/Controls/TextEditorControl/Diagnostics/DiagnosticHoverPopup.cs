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
  private readonly StackPanel _messages = new();
  private readonly Popup _popup;
  private readonly DispatcherTimer _closeTimer;
  private IReadOnlyList<SourceDiagnostic> _diagnostics = Array.Empty<SourceDiagnostic>();

  internal DiagnosticHoverPopup(FrameworkElement owner)
  {
    _owner = owner;
    _border = new Border
    {
      Padding = new Thickness(12, 10, 14, 10),
      BorderThickness = new Thickness(1),
      CornerRadius = new CornerRadius(6),
      MaxWidth = 560,
      Child = new ScrollViewer
      {
        Content = _messages,
        MaxHeight = 320,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        Focusable = false,
      },
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
    if (!_popup.IsOpen || !_diagnostics.SequenceEqual(diagnostics))
    {
      _diagnostics = diagnostics.ToArray();
      _messages.Children.Clear();
      _border.Background = GetBrush("ToolTipBackgroundBrush", Brushes.WhiteSmoke);
      _border.BorderBrush = GetBrush("ToolTipBorderBrush", Brushes.SlateGray);
      var foreground = GetBrush("ToolTipForegroundBrush", Brushes.Black);

      foreach (var diagnostic in diagnostics.DistinctBy(d => (d.IsWarning, d.Description)))
      {
        var text = new StackPanel();
        text.Children.Add(new TextBlock
        {
          Text = diagnostic.IsWarning ? "Предупреждение" : "Ошибка",
          FontWeight = FontWeights.SemiBold,
          Margin = new Thickness(0, 0, 0, 4),
        });
        text.Children.Add(new TextBlock { Text = diagnostic.Description, TextWrapping = TextWrapping.Wrap });
        var entry = new Border
        {
          BorderThickness = new Thickness(3, 0, 0, 0),
          BorderBrush = diagnostic.IsWarning ? Brushes.Gold : Brushes.Red,
          Padding = new Thickness(10, 0, 0, 0),
          Margin = new Thickness(0, _messages.Children.Count == 0 ? 0 : 12, 0, 0),
          Child = text,
        };
        TextElement.SetForeground(entry, foreground);
        TextElement.SetFontFamily(entry, SystemFonts.MessageFontFamily);
        TextElement.SetFontSize(entry, 13);
        _messages.Children.Add(entry);
      }
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
    _messages.Children.Clear();
  }

  private Brush GetBrush(string key, Brush fallback) => _owner.TryFindResource(key) as Brush ?? fallback;
}
