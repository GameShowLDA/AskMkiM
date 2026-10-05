using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UI.Controls.Calendar;

/// <summary>Отображает заголовок дела с анимированным зачёркиванием по строкам.</summary>
public sealed class TaskTitle : Grid
{
  public static readonly DependencyProperty IsCompletedProperty = DependencyProperty.Register(
    nameof(IsCompleted), typeof(bool), typeof(TaskTitle), new PropertyMetadata(false, OnCompletionChanged));
  private static readonly DependencyProperty StrikeProgressProperty = DependencyProperty.Register(
    "StrikeProgress", typeof(double), typeof(TaskTitle), new PropertyMetadata(0d,
      (sender, _) => ((TaskTitle)sender)._overlay.InvalidateVisual()));
  public static readonly DependencyProperty TextProperty = TextBlock.TextProperty.AddOwner(typeof(TaskTitle), new FrameworkPropertyMetadata(string.Empty, OnTextLayoutChanged));
  public static readonly DependencyProperty FontSizeProperty = TextBlock.FontSizeProperty.AddOwner(typeof(TaskTitle), new FrameworkPropertyMetadata(OnTextLayoutChanged));
  public static readonly DependencyProperty FontWeightProperty = TextBlock.FontWeightProperty.AddOwner(typeof(TaskTitle), new FrameworkPropertyMetadata(OnTextLayoutChanged));
  public static readonly DependencyProperty FontFamilyProperty = TextBlock.FontFamilyProperty.AddOwner(typeof(TaskTitle), new FrameworkPropertyMetadata(OnTextLayoutChanged));
  public static readonly DependencyProperty ForegroundProperty = TextBlock.ForegroundProperty.AddOwner(typeof(TaskTitle), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.Inherits, (sender, _) => ((TaskTitle)sender)._overlay?.InvalidateVisual()));
  public static readonly DependencyProperty TextWrappingProperty = TextBlock.TextWrappingProperty.AddOwner(typeof(TaskTitle), new FrameworkPropertyMetadata(OnTextLayoutChanged));
  private readonly TextBlock _text = new();
  private readonly StrikeOverlay _overlay;
  private List<Rect>? _strikeLines;

  public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
  public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
  public FontWeight FontWeight { get => (FontWeight)GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }
  public FontFamily FontFamily { get => (FontFamily)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
  public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
  public TextWrapping TextWrapping { get => (TextWrapping)GetValue(TextWrappingProperty); set => SetValue(TextWrappingProperty, value); }

  /// <summary>Признак выполнения дела.</summary>
  public bool IsCompleted
  {
    get => (bool)GetValue(IsCompletedProperty);
    set => SetValue(IsCompletedProperty, value);
  }

  public TaskTitle()
  {
    _overlay = new StrikeOverlay(this) { IsHitTestVisible = false };
    foreach (var property in new[] { TextProperty, FontSizeProperty, FontWeightProperty, FontFamilyProperty, ForegroundProperty, TextWrappingProperty })
      _text.SetBinding(property, new Binding(property.Name) { Source = this });
    Children.Add(_text);
    Children.Add(_overlay);
    _text.SizeChanged += (_, _) => { _strikeLines = null; _overlay.InvalidateVisual(); };
    Loaded += (_, _) => ApplyCompletion(false);
    Unloaded += (_, _) => BeginAnimation(StrikeProgressProperty, null);
  }

  private static void OnTextLayoutChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
  {
    var title = (TaskTitle)sender;
    title._strikeLines = null;
    title._overlay?.InvalidateVisual();
  }

  private static void OnCompletionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
  {
    var title = (TaskTitle)sender;
    title.ApplyCompletion(title.IsLoaded);
  }

  private void ApplyCompletion(bool animate)
  {
    double target = IsCompleted ? 1d : 0d;
    if (animate)
      BeginAnimation(StrikeProgressProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(250))
      { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } });
    else
    {
      BeginAnimation(StrikeProgressProperty, null);
      SetValue(StrikeProgressProperty, target);
    }
  }

  private void DrawStrike(DrawingContext drawingContext)
  {
    double progress = (double)GetValue(StrikeProgressProperty);
    if (progress <= 0 || Foreground is null) return;
    // Кэш раскладки исключает повторное измерение текста на каждом кадре анимации.
    var lines = _strikeLines ?? new List<Rect>();
    if (_strikeLines is null && !string.IsNullOrEmpty(Text) && _text.ActualWidth > 0)
    {
      var formatted = new FormattedText(Text, System.Globalization.CultureInfo.CurrentUICulture,
        FlowDirection, new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal),
        FontSize, Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);
      if (TextWrapping != TextWrapping.NoWrap) formatted.MaxTextWidth = _text.ActualWidth;
      for (int index = 0; index < Text.Length; index++)
      {
        var geometry = formatted.BuildHighlightGeometry(new Point(), index, 1);
        if (geometry is null || geometry.Bounds.IsEmpty) continue;
        var rect = geometry.Bounds;
        int lineIndex = lines.FindIndex(line => Math.Abs(line.Top - rect.Top) < 1);
        if (lineIndex < 0) lines.Add(rect);
        else { var line = lines[lineIndex]; line.Union(rect); lines[lineIndex] = line; }
      }
      _strikeLines = lines;
    }
    var pen = new Pen(Foreground, 1);
    foreach (var line in lines)
    {
      double y = line.Top + line.Height * 0.5;
      drawingContext.DrawLine(pen, new Point(line.Left, y), new Point(line.Left + line.Width * progress, y));
    }
  }

  private sealed class StrikeOverlay(TaskTitle owner) : FrameworkElement
  {
    protected override void OnRender(DrawingContext drawingContext) => owner.DrawStrike(drawingContext);
  }
}
