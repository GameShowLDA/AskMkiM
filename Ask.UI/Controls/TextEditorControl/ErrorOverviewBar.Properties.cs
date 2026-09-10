using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Ask.UI.Controls.TextEditorControl
{
  public sealed partial class ErrorOverviewBar
  {
    private static Brush BrushFrom(string value) => (Brush)new BrushConverter().ConvertFromInvariantString(value)!;

    private static void OnAppearanceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
      var bar = (ErrorOverviewBar)sender;
      if (bar.OverviewSurface == null) return;
      if (e.Property == MarkerHeightProperty || e.Property == MarkerGapProperty ||
          e.Property == IsMarkerGroupingEnabledProperty || e.Property == AreCommandMarkersVisibleProperty ||
          e.Property == AreWarningMarkersVisibleProperty || e.Property == AreErrorMarkersVisibleProperty)
        bar.RebuildMarkers();
      else
        bar.OverviewSurface.InvalidateVisual();
      if (!bar.AreToolTipsEnabled)
        bar._previewPopup.IsOpen = false;
    }

    private static bool IsNonNegative(object value) => value is double number && double.IsFinite(number) && number >= 0;
    private static bool IsPositive(object value) => value is double number && double.IsFinite(number) && number > 0;
    private static bool IsFinite(object value) => value is double number && double.IsFinite(number);
    private static bool IsOpacity(object value) => value is double number && double.IsFinite(number) && number >= 0 && number <= 1;

    /// <summary>
    /// Идентификатор свойства <see cref="ErrorBrush"/>.
    /// </summary>
    public static readonly DependencyProperty ErrorBrushProperty = DependencyProperty.Register(
      nameof(ErrorBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть ошибок; null выбирает ресурс темы.
    /// </summary>
    public Brush? ErrorBrush
    {
      get => (Brush?)GetValue(ErrorBrushProperty);
      set => SetValue(ErrorBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="WarningBrush"/>.
    /// </summary>
    public static readonly DependencyProperty WarningBrushProperty = DependencyProperty.Register(
      nameof(WarningBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть предупреждений; null выбирает ресурс темы.
    /// </summary>
    public Brush? WarningBrush
    {
      get => (Brush?)GetValue(WarningBrushProperty);
      set => SetValue(WarningBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="CommandBrush"/>.
    /// </summary>
    public static readonly DependencyProperty CommandBrushProperty = DependencyProperty.Register(
      nameof(CommandBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть команд и информационных маркеров.
    /// </summary>
    public Brush CommandBrush
    {
      get => (Brush)GetValue(CommandBrushProperty);
      set => SetValue(CommandBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="ViewportBrush"/>.
    /// </summary>
    public static readonly DependencyProperty ViewportBrushProperty = DependencyProperty.Register(
      nameof(ViewportBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть видимой области; null использует цвет её рамки.
    /// </summary>
    public Brush? ViewportBrush
    {
      get => (Brush?)GetValue(ViewportBrushProperty);
      set => SetValue(ViewportBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="ViewportBorderBrush"/>.
    /// </summary>
    public static readonly DependencyProperty ViewportBorderBrushProperty = DependencyProperty.Register(
      nameof(ViewportBorderBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть рамки видимой области; null выбирает ресурс темы.
    /// </summary>
    public Brush? ViewportBorderBrush
    {
      get => (Brush?)GetValue(ViewportBorderBrushProperty);
      set => SetValue(ViewportBorderBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="MarkerBorderBrush"/>.
    /// </summary>
    public static readonly DependencyProperty MarkerBorderBrushProperty = DependencyProperty.Register(
      nameof(MarkerBorderBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть рамки обычного маркера.
    /// </summary>
    public Brush? MarkerBorderBrush
    {
      get => (Brush?)GetValue(MarkerBorderBrushProperty);
      set => SetValue(MarkerBorderBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="ActiveMarkerBrush"/>.
    /// </summary>
    public static readonly DependencyProperty ActiveMarkerBrushProperty = DependencyProperty.Register(
      nameof(ActiveMarkerBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть активного маркера; null сохраняет цвет категории.
    /// </summary>
    public Brush? ActiveMarkerBrush
    {
      get => (Brush?)GetValue(ActiveMarkerBrushProperty);
      set => SetValue(ActiveMarkerBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="HoverMarkerBrush"/>.
    /// </summary>
    public static readonly DependencyProperty HoverMarkerBrushProperty = DependencyProperty.Register(
      nameof(HoverMarkerBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть маркера под указателем; null сохраняет цвет категории.
    /// </summary>
    public Brush? HoverMarkerBrush
    {
      get => (Brush?)GetValue(HoverMarkerBrushProperty);
      set => SetValue(HoverMarkerBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="ActiveMarkerBorderBrush"/>.
    /// </summary>
    public static readonly DependencyProperty ActiveMarkerBorderBrushProperty = DependencyProperty.Register(
      nameof(ActiveMarkerBorderBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть рамки активного маркера; null использует рамку видимой области.
    /// </summary>
    public Brush? ActiveMarkerBorderBrush
    {
      get => (Brush?)GetValue(ActiveMarkerBorderBrushProperty);
      set => SetValue(ActiveMarkerBorderBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="HoverMarkerBorderBrush"/>.
    /// </summary>
    public static readonly DependencyProperty HoverMarkerBorderBrushProperty = DependencyProperty.Register(
      nameof(HoverMarkerBorderBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть рамки маркера под указателем.
    /// </summary>
    public Brush? HoverMarkerBorderBrush
    {
      get => (Brush?)GetValue(HoverMarkerBorderBrushProperty);
      set => SetValue(HoverMarkerBorderBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewBackground"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewBackgroundProperty = DependencyProperty.Register(
      nameof(PreviewBackground), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(BrushFrom("#F51C222B"), FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Фон подсказки.
    /// </summary>
    public Brush PreviewBackground
    {
      get => (Brush)GetValue(PreviewBackgroundProperty);
      set => SetValue(PreviewBackgroundProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewForeground"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewForegroundProperty = DependencyProperty.Register(
      nameof(PreviewForeground), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(Brushes.WhiteSmoke, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Цвет текста подсказки.
    /// </summary>
    public Brush PreviewForeground
    {
      get => (Brush)GetValue(PreviewForegroundProperty);
      set => SetValue(PreviewForegroundProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewBorderBrush"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewBorderBrushProperty = DependencyProperty.Register(
      nameof(PreviewBorderBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(BrushFrom("#FF485B71"), FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть рамки подсказки.
    /// </summary>
    public Brush PreviewBorderBrush
    {
      get => (Brush)GetValue(PreviewBorderBrushProperty);
      set => SetValue(PreviewBorderBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewAccentBrush"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewAccentBrushProperty = DependencyProperty.Register(
      nameof(PreviewAccentBrush), typeof(Brush), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(BrushFrom("#FF2992FF"), FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Кисть акцента подсказки.
    /// </summary>
    public Brush PreviewAccentBrush
    {
      get => (Brush)GetValue(PreviewAccentBrushProperty);
      set => SetValue(PreviewAccentBrushProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="MarkerHeight"/>.
    /// </summary>
    public static readonly DependencyProperty MarkerHeightProperty = DependencyProperty.Register(
      nameof(MarkerHeight), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsPositive);

    /// <summary>
    /// Высота обычного маркера.
    /// </summary>
    public double MarkerHeight
    {
      get => (double)GetValue(MarkerHeightProperty);
      set => SetValue(MarkerHeightProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="MarkerGap"/>.
    /// </summary>
    public static readonly DependencyProperty MarkerGapProperty = DependencyProperty.Register(
      nameof(MarkerGap), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Расстояние между маркерами и порог группировки.
    /// </summary>
    public double MarkerGap
    {
      get => (double)GetValue(MarkerGapProperty);
      set => SetValue(MarkerGapProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="ActiveMarkerHeight"/>.
    /// </summary>
    public static readonly DependencyProperty ActiveMarkerHeightProperty = DependencyProperty.Register(
      nameof(ActiveMarkerHeight), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsPositive);

    /// <summary>
    /// Высота активного маркера.
    /// </summary>
    public double ActiveMarkerHeight
    {
      get => (double)GetValue(ActiveMarkerHeightProperty);
      set => SetValue(ActiveMarkerHeightProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="HoverMarkerHeight"/>.
    /// </summary>
    public static readonly DependencyProperty HoverMarkerHeightProperty = DependencyProperty.Register(
      nameof(HoverMarkerHeight), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsPositive);

    /// <summary>
    /// Высота маркера под указателем.
    /// </summary>
    public double HoverMarkerHeight
    {
      get => (double)GetValue(HoverMarkerHeightProperty);
      set => SetValue(HoverMarkerHeightProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="MarkerInset"/>.
    /// </summary>
    public static readonly DependencyProperty MarkerInsetProperty = DependencyProperty.Register(
      nameof(MarkerInset), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Горизонтальный отступ маркеров.
    /// </summary>
    public double MarkerInset
    {
      get => (double)GetValue(MarkerInsetProperty);
      set => SetValue(MarkerInsetProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="MarkerHitPadding"/>.
    /// </summary>
    public static readonly DependencyProperty MarkerHitPaddingProperty = DependencyProperty.Register(
      nameof(MarkerHitPadding), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Допуск попадания указателя по вертикали.
    /// </summary>
    public double MarkerHitPadding
    {
      get => (double)GetValue(MarkerHitPaddingProperty);
      set => SetValue(MarkerHitPaddingProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="MarkerCornerRadius"/>.
    /// </summary>
    public static readonly DependencyProperty MarkerCornerRadiusProperty = DependencyProperty.Register(
      nameof(MarkerCornerRadius), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Радиус углов маркеров.
    /// </summary>
    public double MarkerCornerRadius
    {
      get => (double)GetValue(MarkerCornerRadiusProperty);
      set => SetValue(MarkerCornerRadiusProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="TrackCornerRadius"/>.
    /// </summary>
    public static readonly DependencyProperty TrackCornerRadiusProperty = DependencyProperty.Register(
      nameof(TrackCornerRadius), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Радиус углов фона полосы.
    /// </summary>
    public double TrackCornerRadius
    {
      get => (double)GetValue(TrackCornerRadiusProperty);
      set => SetValue(TrackCornerRadiusProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="ViewportCornerRadius"/>.
    /// </summary>
    public static readonly DependencyProperty ViewportCornerRadiusProperty = DependencyProperty.Register(
      nameof(ViewportCornerRadius), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Радиус углов видимой области.
    /// </summary>
    public double ViewportCornerRadius
    {
      get => (double)GetValue(ViewportCornerRadiusProperty);
      set => SetValue(ViewportCornerRadiusProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="MarkerBorderThickness"/>.
    /// </summary>
    public static readonly DependencyProperty MarkerBorderThicknessProperty = DependencyProperty.Register(
      nameof(MarkerBorderThickness), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Толщина рамки маркера.
    /// </summary>
    public double MarkerBorderThickness
    {
      get => (double)GetValue(MarkerBorderThicknessProperty);
      set => SetValue(MarkerBorderThicknessProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="ActiveMarkerBorderThickness"/>.
    /// </summary>
    public static readonly DependencyProperty ActiveMarkerBorderThicknessProperty = DependencyProperty.Register(
      nameof(ActiveMarkerBorderThickness), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Толщина рамки активного маркера.
    /// </summary>
    public double ActiveMarkerBorderThickness
    {
      get => (double)GetValue(ActiveMarkerBorderThicknessProperty);
      set => SetValue(ActiveMarkerBorderThicknessProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="HoverMarkerBorderThickness"/>.
    /// </summary>
    public static readonly DependencyProperty HoverMarkerBorderThicknessProperty = DependencyProperty.Register(
      nameof(HoverMarkerBorderThickness), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Толщина рамки маркера под указателем.
    /// </summary>
    public double HoverMarkerBorderThickness
    {
      get => (double)GetValue(HoverMarkerBorderThicknessProperty);
      set => SetValue(HoverMarkerBorderThicknessProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="ViewportBorderThickness"/>.
    /// </summary>
    public static readonly DependencyProperty ViewportBorderThicknessProperty = DependencyProperty.Register(
      nameof(ViewportBorderThickness), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Толщина рамки видимой области.
    /// </summary>
    public double ViewportBorderThickness
    {
      get => (double)GetValue(ViewportBorderThicknessProperty);
      set => SetValue(ViewportBorderThicknessProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="ViewportOpacity"/>.
    /// </summary>
    public static readonly DependencyProperty ViewportOpacityProperty = DependencyProperty.Register(
      nameof(ViewportOpacity), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(0.14, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsOpacity);

    /// <summary>
    /// Непрозрачность заливки видимой области от нуля до единицы.
    /// </summary>
    public double ViewportOpacity
    {
      get => (double)GetValue(ViewportOpacityProperty);
      set => SetValue(ViewportOpacityProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewWidth"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewWidthProperty = DependencyProperty.Register(
      nameof(PreviewWidth), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(680.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsPositive);

    /// <summary>
    /// Ширина подсказки.
    /// </summary>
    public double PreviewWidth
    {
      get => (double)GetValue(PreviewWidthProperty);
      set => SetValue(PreviewWidthProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewFontSize"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewFontSizeProperty = DependencyProperty.Register(
      nameof(PreviewFontSize), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(12.5, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsPositive);

    /// <summary>
    /// Размер шрифта подсказки.
    /// </summary>
    public double PreviewFontSize
    {
      get => (double)GetValue(PreviewFontSizeProperty);
      set => SetValue(PreviewFontSizeProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewLineHeight"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewLineHeightProperty = DependencyProperty.Register(
      nameof(PreviewLineHeight), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsPositive);

    /// <summary>
    /// Высота строки подсказки.
    /// </summary>
    public double PreviewLineHeight
    {
      get => (double)GetValue(PreviewLineHeightProperty);
      set => SetValue(PreviewLineHeightProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewHorizontalGap"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewHorizontalGapProperty = DependencyProperty.Register(
      nameof(PreviewHorizontalGap), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Расстояние от подсказки до полосы.
    /// </summary>
    public double PreviewHorizontalGap
    {
      get => (double)GetValue(PreviewHorizontalGapProperty);
      set => SetValue(PreviewHorizontalGapProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewVerticalOffset"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewVerticalOffsetProperty = DependencyProperty.Register(
      nameof(PreviewVerticalOffset), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(-22.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsFinite);

    /// <summary>
    /// Смещение подсказки относительно указателя по вертикали.
    /// </summary>
    public double PreviewVerticalOffset
    {
      get => (double)GetValue(PreviewVerticalOffsetProperty);
      set => SetValue(PreviewVerticalOffsetProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewAccentWidth"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewAccentWidthProperty = DependencyProperty.Register(
      nameof(PreviewAccentWidth), typeof(double), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged), IsNonNegative);

    /// <summary>
    /// Ширина цветового акцента подсказки.
    /// </summary>
    public double PreviewAccentWidth
    {
      get => (double)GetValue(PreviewAccentWidthProperty);
      set => SetValue(PreviewAccentWidthProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="IsViewportVisible"/>.
    /// </summary>
    public static readonly DependencyProperty IsViewportVisibleProperty = DependencyProperty.Register(
      nameof(IsViewportVisible), typeof(bool), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Видимость индикатора области прокрутки.
    /// </summary>
    public bool IsViewportVisible
    {
      get => (bool)GetValue(IsViewportVisibleProperty);
      set => SetValue(IsViewportVisibleProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="IsNavigationEnabled"/>.
    /// </summary>
    public static readonly DependencyProperty IsNavigationEnabledProperty = DependencyProperty.Register(
      nameof(IsNavigationEnabled), typeof(bool), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Разрешение переходов кликом по полосе.
    /// </summary>
    public bool IsNavigationEnabled
    {
      get => (bool)GetValue(IsNavigationEnabledProperty);
      set => SetValue(IsNavigationEnabledProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="IsTrackNavigationEnabled"/>.
    /// </summary>
    public static readonly DependencyProperty IsTrackNavigationEnabledProperty = DependencyProperty.Register(
      nameof(IsTrackNavigationEnabled), typeof(bool), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Разрешение прокрутки кликом по свободной области.
    /// </summary>
    public bool IsTrackNavigationEnabled
    {
      get => (bool)GetValue(IsTrackNavigationEnabledProperty);
      set => SetValue(IsTrackNavigationEnabledProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="AreToolTipsEnabled"/>.
    /// </summary>
    public static readonly DependencyProperty AreToolTipsEnabledProperty = DependencyProperty.Register(
      nameof(AreToolTipsEnabled), typeof(bool), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Разрешение показа подсказок.
    /// </summary>
    public bool AreToolTipsEnabled
    {
      get => (bool)GetValue(AreToolTipsEnabledProperty);
      set => SetValue(AreToolTipsEnabledProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="IsPositionPreviewEnabled"/>.
    /// </summary>
    public static readonly DependencyProperty IsPositionPreviewEnabledProperty = DependencyProperty.Register(
      nameof(IsPositionPreviewEnabled), typeof(bool), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Добавление фрагмента протокола в подсказку.
    /// </summary>
    public bool IsPositionPreviewEnabled
    {
      get => (bool)GetValue(IsPositionPreviewEnabledProperty);
      set => SetValue(IsPositionPreviewEnabledProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="AreCommandMarkersVisible"/>.
    /// </summary>
    public static readonly DependencyProperty AreCommandMarkersVisibleProperty = DependencyProperty.Register(
      nameof(AreCommandMarkersVisible), typeof(bool), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Видимость маркеров команд.
    /// </summary>
    public bool AreCommandMarkersVisible
    {
      get => (bool)GetValue(AreCommandMarkersVisibleProperty);
      set => SetValue(AreCommandMarkersVisibleProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="AreWarningMarkersVisible"/>.
    /// </summary>
    public static readonly DependencyProperty AreWarningMarkersVisibleProperty = DependencyProperty.Register(
      nameof(AreWarningMarkersVisible), typeof(bool), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Видимость маркеров предупреждений.
    /// </summary>
    public bool AreWarningMarkersVisible
    {
      get => (bool)GetValue(AreWarningMarkersVisibleProperty);
      set => SetValue(AreWarningMarkersVisibleProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="AreErrorMarkersVisible"/>.
    /// </summary>
    public static readonly DependencyProperty AreErrorMarkersVisibleProperty = DependencyProperty.Register(
      nameof(AreErrorMarkersVisible), typeof(bool), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Видимость маркеров ошибок.
    /// </summary>
    public bool AreErrorMarkersVisible
    {
      get => (bool)GetValue(AreErrorMarkersVisibleProperty);
      set => SetValue(AreErrorMarkersVisibleProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="IsMarkerGroupingEnabled"/>.
    /// </summary>
    public static readonly DependencyProperty IsMarkerGroupingEnabledProperty = DependencyProperty.Register(
      nameof(IsMarkerGroupingEnabled), typeof(bool), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Объединение близких маркеров в группы.
    /// </summary>
    public bool IsMarkerGroupingEnabled
    {
      get => (bool)GetValue(IsMarkerGroupingEnabledProperty);
      set => SetValue(IsMarkerGroupingEnabledProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="CornerRadius"/>.
    /// </summary>
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
      nameof(CornerRadius), typeof(CornerRadius), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(new CornerRadius(5), FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Радиусы углов внешней рамки.
    /// </summary>
    public CornerRadius CornerRadius
    {
      get => (CornerRadius)GetValue(CornerRadiusProperty);
      set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewCornerRadius"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewCornerRadiusProperty = DependencyProperty.Register(
      nameof(PreviewCornerRadius), typeof(CornerRadius), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(new CornerRadius(6), FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Радиусы углов подсказки.
    /// </summary>
    public CornerRadius PreviewCornerRadius
    {
      get => (CornerRadius)GetValue(PreviewCornerRadiusProperty);
      set => SetValue(PreviewCornerRadiusProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewPadding"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewPaddingProperty = DependencyProperty.Register(
      nameof(PreviewPadding), typeof(Thickness), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(new Thickness(11, 8, 12, 8), FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Внутренние отступы подсказки.
    /// </summary>
    public Thickness PreviewPadding
    {
      get => (Thickness)GetValue(PreviewPaddingProperty);
      set => SetValue(PreviewPaddingProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewBorderThickness"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewBorderThicknessProperty = DependencyProperty.Register(
      nameof(PreviewBorderThickness), typeof(Thickness), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(new Thickness(1), FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Толщина рамки подсказки.
    /// </summary>
    public Thickness PreviewBorderThickness
    {
      get => (Thickness)GetValue(PreviewBorderThicknessProperty);
      set => SetValue(PreviewBorderThicknessProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewAccentMargin"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewAccentMarginProperty = DependencyProperty.Register(
      nameof(PreviewAccentMargin), typeof(Thickness), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(new Thickness(0, 1, 9, 1), FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Отступы цветового акцента.
    /// </summary>
    public Thickness PreviewAccentMargin
    {
      get => (Thickness)GetValue(PreviewAccentMarginProperty);
      set => SetValue(PreviewAccentMarginProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewFontFamily"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewFontFamilyProperty = DependencyProperty.Register(
      nameof(PreviewFontFamily), typeof(FontFamily), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(new FontFamily("Consolas"), FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Шрифт подсказки.
    /// </summary>
    public FontFamily PreviewFontFamily
    {
      get => (FontFamily)GetValue(PreviewFontFamilyProperty);
      set => SetValue(PreviewFontFamilyProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewTextWrapping"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewTextWrappingProperty = DependencyProperty.Register(
      nameof(PreviewTextWrapping), typeof(TextWrapping), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(TextWrapping.NoWrap, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Перенос строк подсказки.
    /// </summary>
    public TextWrapping PreviewTextWrapping
    {
      get => (TextWrapping)GetValue(PreviewTextWrappingProperty);
      set => SetValue(PreviewTextWrappingProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewContentTemplate"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewContentTemplateProperty = DependencyProperty.Register(
      nameof(PreviewContentTemplate), typeof(DataTemplate), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Шаблон содержимого подсказки; контекст — строка предпросмотра.
    /// </summary>
    public DataTemplate? PreviewContentTemplate
    {
      get => (DataTemplate?)GetValue(PreviewContentTemplateProperty);
      set => SetValue(PreviewContentTemplateProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="PreviewEffect"/>.
    /// </summary>
    public static readonly DependencyProperty PreviewEffectProperty = DependencyProperty.Register(
      nameof(PreviewEffect), typeof(Effect), typeof(ErrorOverviewBar),
      new FrameworkPropertyMetadata(new DropShadowEffect { Color = Colors.Black, BlurRadius = 14, ShadowDepth = 3, Opacity = 0.45 }, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    /// <summary>
    /// Эффект подсказки; null отключает эффект.
    /// </summary>
    public Effect PreviewEffect
    {
      get => (Effect)GetValue(PreviewEffectProperty);
      set => SetValue(PreviewEffectProperty, value);
    }

  }
}
