using AvalonEditor = ICSharpCode.AvalonEdit.TextEditor;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Ask.UI.Shared.Search;

/// <summary>Решение по одной замене с отслеживаемым диапазоном, без общего Undo документа.</summary>
public sealed class ReplacementReview
{
  private readonly AvalonEditor _editor;
  private readonly TextDocument _document;
  private readonly string _original;
  private readonly string _replacement;
  private readonly Func<bool> _isActive;
  private readonly Action _dismiss;

  public ReplacementReview(AvalonEditor editor, TextSegment segment, string original, string replacement,
    Func<bool> isActive, Action dismiss)
  {
    _editor = editor;
    _document = editor.Document;
    Segment = segment;
    _original = original;
    _replacement = replacement;
    _isActive = isActive;
    _dismiss = dismiss;
  }

  public TextSegment Segment { get; }
  public bool IsActive => ReferenceEquals(_document, _editor.Document) && _isActive();
  public bool CanCancel => IsActive && !_editor.IsReadOnly && Segment.Length == _replacement.Length
    && Segment.StartOffset >= 0 && Segment.EndOffset <= _document.TextLength
    && _document.GetText(Segment.StartOffset, Segment.Length) == _replacement;

  public bool Resolve(bool cancel)
  {
    if (!IsActive || cancel && !CanCancel) return false;
    int offset = Segment.StartOffset;
    int length = Segment.Length;
    _dismiss();
    if (cancel) _document.Replace(offset, length, _original);
    return true;
  }
}

/// <summary>Открывает действия подтверждения/отмены при наведении на рамку замены.</summary>
public sealed class ReplacementReviewMenu
{
  private readonly AvalonEditor _editor;
  private readonly Func<IEnumerable<ReplacementReview>> _reviews;
  private ContextMenu? _menu;

  public ReplacementReviewMenu(AvalonEditor editor, Func<IEnumerable<ReplacementReview>> reviews)
  {
    _editor = editor;
    _reviews = reviews;
    editor.AddHandler(AvalonEditor.MouseHoverEvent, new MouseEventHandler(OnMouseHover), true);
    editor.Unloaded += (_, _) => Close();
    editor.DocumentChanged += (_, _) => Close();
  }

  public void Close()
  {
    if (_menu != null) _menu.IsOpen = false;
    _menu = null;
  }

  private static MenuItem CreateCompactItem(string header) => new()
  {
    Header = header,
    Height = 22,
    MinHeight = 0,
    Padding = new Thickness(8, 2, 8, 2),
    Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse("""
      <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                       xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="MenuItem">
        <Border x:Name="Surface" Background="Transparent" CornerRadius="3" Padding="{TemplateBinding Padding}">
          <ContentPresenter x:Name="HeaderPresenter" ContentSource="Header" RecognizesAccessKey="True" VerticalAlignment="Center"/>
        </Border>
        <ControlTemplate.Triggers>
          <Trigger Property="IsHighlighted" Value="True">
            <Setter TargetName="Surface" Property="Background" Value="{DynamicResource IsCheckedColorSolidColorBrush}"/>
          </Trigger>
          <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Surface" Property="Opacity" Value="0.4"/></Trigger>
        </ControlTemplate.Triggers>
      </ControlTemplate>
      """),
    FontSize = 12,
    FontWeight = FontWeights.Normal,
    FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
  };

  private void OnMouseHover(object sender, MouseEventArgs e)
  {
    if (TryOpen(e.GetPosition(_editor.TextArea.TextView))) e.Handled = true;
  }

  internal ContextMenu? Menu => _menu;

  internal bool TryOpen(Point point)
  {
    if (_menu?.IsOpen == true) return false;
    var view = _editor.TextArea.TextView;
    if (!view.VisualLinesValid) return false;
    foreach (var review in _reviews().Reverse())
    {
      if (!review.IsActive || review.Segment.Length <= 0) continue;
      var rectangle = BackgroundGeometryBuilder.GetRectsForSegment(view, review.Segment)
        .Where(rect => { rect.Inflate(2, 2); return rect.Contains(point); }).FirstOrDefault();
      if (rectangle.IsEmpty || rectangle.Width <= 0) continue;
      var menu = new ContextMenu
      {
        PlacementTarget = view,
        Placement = PlacementMode.Bottom,
        PlacementRectangle = rectangle,
        StaysOpen = false,
        MinWidth = 100,
        FontSize = 12,
        FontWeight = FontWeights.Normal,
        FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
      };
      if (_editor.TryFindResource("AppContextMenuStyle") is Style style) menu.Style = style;
      var confirm = CreateCompactItem("Подтвердить");
      var cancel = CreateCompactItem("Отменить");
      cancel.IsEnabled = review.CanCancel;
      if (!cancel.IsEnabled) cancel.ToolTip = "Фрагмент изменён после замены или доступен только для чтения";
      confirm.Click += (_, _) => { review.Resolve(false); Close(); };
      cancel.Click += (_, _) => { review.Resolve(true); Close(); };
      menu.Items.Add(confirm);
      menu.Items.Add(cancel);
      _menu = menu;
      menu.IsOpen = true;
      return true;
    }
    return false;
  }
}
