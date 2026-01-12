using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UI.Icon
{
  /// <summary>
  /// Компактный индикатор успеха: круг с галочкой.
  /// Поддерживает настраиваемые цвета/толщины и анимацию «прорисовки» галочки.
  /// </summary>
  public partial class SuccessIconControlV2 : UserControl
  {
    public SuccessIconControlV2()
    {
      InitializeComponent();
      Loaded += (_, __) => UpdateVisual(animate: false);
    }

    // === Внешний вид ===

    /// <summary>Фон кругового индикатора.</summary>
    public Brush CircleBackground
    {
      get => (Brush)GetValue(CircleBackgroundProperty);
      set => SetValue(CircleBackgroundProperty, value);
    }
    /// <summary>DP для <see cref="CircleBackground"/>. Значение по умолчанию: #2A3848.</summary>
    public static readonly DependencyProperty CircleBackgroundProperty =
      DependencyProperty.Register(nameof(CircleBackground), typeof(Brush), typeof(SuccessIconControlV2),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x2A, 0x38, 0x48))));

    /// <summary>Цвет обводки круга.</summary>
    public Brush CircleBorder
    {
      get => (Brush)GetValue(CircleBorderProperty);
      set => SetValue(CircleBorderProperty, value);
    }
    /// <summary>DP для <see cref="CircleBorder"/>. Значение по умолчанию: #425263.</summary>
    public static readonly DependencyProperty CircleBorderProperty =
      DependencyProperty.Register(nameof(CircleBorder), typeof(Brush), typeof(SuccessIconControlV2),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x42, 0x52, 0x63))));

    /// <summary>Толщина обводки круга в единицах устройства.</summary>
    public double CircleBorderThickness
    {
      get => (double)GetValue(CircleBorderThicknessProperty);
      set => SetValue(CircleBorderThicknessProperty, value);
    }
    /// <summary>DP для <see cref="CircleBorderThickness"/>. Значение по умолчанию: 1.0.</summary>
    public static readonly DependencyProperty CircleBorderThicknessProperty =
      DependencyProperty.Register(nameof(CircleBorderThickness), typeof(double), typeof(SuccessIconControlV2),
        new PropertyMetadata(1.0));

    /// <summary>Цвет линии галочки.</summary>
    public Brush CheckBrush
    {
      get => (Brush)GetValue(CheckBrushProperty);
      set => SetValue(CheckBrushProperty, value);
    }
    /// <summary>DP для <see cref="CheckBrush"/>. Значение по умолчанию: #67C66E.</summary>
    public static readonly DependencyProperty CheckBrushProperty =
      DependencyProperty.Register(nameof(CheckBrush), typeof(Brush), typeof(SuccessIconControlV2),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x67, 0xC6, 0x6E))));

    /// <summary>Толщина линии галочки.</summary>
    public double CheckThickness
    {
      get => (double)GetValue(CheckThicknessProperty);
      set => SetValue(CheckThicknessProperty, value);
    }
    /// <summary>DP для <see cref="CheckThickness"/>. Значение по умолчанию: 2.0.</summary>
    public static readonly DependencyProperty CheckThicknessProperty =
      DependencyProperty.Register(nameof(CheckThickness), typeof(double), typeof(SuccessIconControlV2),
        new PropertyMetadata(2.0));

    // === Состояние и анимация ===

    /// <summary>
    /// Флаг активности иконки. Если <c>true</c> — галочка отображается (с анимацией при включённом <see cref="UseAnimation"/>);
    /// если <c>false</c> — скрывается.
    /// </summary>
    public bool IsActive
    {
      get => (bool)GetValue(IsActiveProperty);
      set => SetValue(IsActiveProperty, value);
    }
    /// <summary>DP для <see cref="IsActive"/>. Значение по умолчанию: <c>true</c>.</summary>
    public static readonly DependencyProperty IsActiveProperty =
      DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(SuccessIconControlV2),
        new PropertyMetadata(true, OnIsActiveChanged));

    /// <summary>
    /// Включает/выключает анимацию «прорисовки» галочки при смене <see cref="IsActive"/>.
    /// </summary>
    public bool UseAnimation
    {
      get => (bool)GetValue(UseAnimationProperty);
      set => SetValue(UseAnimationProperty, value);
    }
    /// <summary>DP для <see cref="UseAnimation"/>. Значение по умолчанию: <c>true</c>.</summary>
    public static readonly DependencyProperty UseAnimationProperty =
      DependencyProperty.Register(nameof(UseAnimation), typeof(bool), typeof(SuccessIconControlV2),
        new PropertyMetadata(true));

    /// <summary>Обработчик изменения <see cref="IsActive"/> — обновляет визуальное состояние, при необходимости с анимацией.</summary>
    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ((SuccessIconControlV2)d).UpdateVisual(animate: true);
    }

    /// <summary>
    /// Обновляет видимость галочки. При <paramref name="animate"/> и включённом <see cref="UseAnimation"/>
    /// выполняет анимацию изменения <c>StrokeDashOffset</c> (эффект «рисования»).
    /// </summary>
    private void UpdateVisual(bool animate)
    {
      if (Tick == null) return;

      if (!IsActive)
      {
        // Скрыть галочку
        if (animate && UseAnimation)
          BeginDashAnimation(toOffset: 200, durationMs: 140);
        else
          Tick.StrokeDashOffset = 200;
        return;
      }

      // Показать галочку
      if (animate && UseAnimation)
        BeginDashAnimation(toOffset: 0, durationMs: 180, fromOffset: 200);
      else
        Tick.StrokeDashOffset = 0;
    }

    /// <summary>
    /// Запускает анимацию сдвига штриховки галочки.
    /// </summary>
    /// <param name="toOffset">Конечное значение <c>StrokeDashOffset</c>.</param>
    /// <param name="durationMs">Длительность анимации в миллисекундах.</param>
    /// <param name="fromOffset">Начальное значение; если не задано — берётся текущее.</param>
    private void BeginDashAnimation(double toOffset, int durationMs, double? fromOffset = null)
    {
      var anim = new DoubleAnimation
      {
        To = toOffset,
        Duration = TimeSpan.FromMilliseconds(durationMs),
        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
      };
      if (fromOffset.HasValue) anim.From = fromOffset.Value;

      Tick.BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty, anim);
    }
  }
}
