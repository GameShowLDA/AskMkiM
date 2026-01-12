using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UI.Icon
{
  /// <summary>
  /// Компактный индикатор ошибки: круг с крестиком.
  /// Поддерживает настраиваемые цвета/толщины и анимацию «прорисовки» линий.
  /// </summary>
  public partial class ErrorIconControlV2 : UserControl
  {
    public ErrorIconControlV2()
    {
      InitializeComponent();
      Loaded += (_, __) => UpdateVisual(animate: false);
    }

    // ===== Внешний вид =====
    /// <summary>Фон кругового индикатора.</summary>
    public Brush CircleBackground
    {
      get => (Brush)GetValue(CircleBackgroundProperty);
      set => SetValue(CircleBackgroundProperty, value);
    }
    public static readonly DependencyProperty CircleBackgroundProperty =
      DependencyProperty.Register(nameof(CircleBackground), typeof(Brush), typeof(ErrorIconControlV2),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x2A, 0x38, 0x48)))); // #2A3848

    /// <summary>Цвет обводки круга.</summary>
    public Brush CircleBorder
    {
      get => (Brush)GetValue(CircleBorderProperty);
      set => SetValue(CircleBorderProperty, value);
    }
    public static readonly DependencyProperty CircleBorderProperty =
      DependencyProperty.Register(nameof(CircleBorder), typeof(Brush), typeof(ErrorIconControlV2),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x42, 0x52, 0x63)))); // #425263

    /// <summary>Толщина обводки круга.</summary>
    public double CircleBorderThickness
    {
      get => (double)GetValue(CircleBorderThicknessProperty);
      set => SetValue(CircleBorderThicknessProperty, value);
    }
    public static readonly DependencyProperty CircleBorderThicknessProperty =
      DependencyProperty.Register(nameof(CircleBorderThickness), typeof(double), typeof(ErrorIconControlV2),
        new PropertyMetadata(1.0));

    /// <summary>Цвет линий крестика.</summary>
    public Brush CrossBrush
    {
      get => (Brush)GetValue(CrossBrushProperty);
      set => SetValue(CrossBrushProperty, value);
    }
    public static readonly DependencyProperty CrossBrushProperty =
      DependencyProperty.Register(nameof(CrossBrush), typeof(Brush), typeof(ErrorIconControlV2),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xF0, 0x71, 0x6A)))); // #F0716A

    /// <summary>Толщина линий крестика.</summary>
    public double CrossThickness
    {
      get => (double)GetValue(CrossThicknessProperty);
      set => SetValue(CrossThicknessProperty, value);
    }
    public static readonly DependencyProperty CrossThicknessProperty =
      DependencyProperty.Register(nameof(CrossThickness), typeof(double), typeof(ErrorIconControlV2),
        new PropertyMetadata(2.0));

    // ===== Состояние и анимация =====
    /// <summary>Активен ли индикатор. True — крестик виден, False — скрыт.</summary>
    public bool IsActive
    {
      get => (bool)GetValue(IsActiveProperty);
      set => SetValue(IsActiveProperty, value);
    }
    public static readonly DependencyProperty IsActiveProperty =
      DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(ErrorIconControlV2),
        new PropertyMetadata(true, OnIsActiveChanged));

    /// <summary>Включить/выключить анимацию прорисовки при смене состояния.</summary>
    public bool UseAnimation
    {
      get => (bool)GetValue(UseAnimationProperty);
      set => SetValue(UseAnimationProperty, value);
    }
    public static readonly DependencyProperty UseAnimationProperty =
      DependencyProperty.Register(nameof(UseAnimation), typeof(bool), typeof(ErrorIconControlV2),
        new PropertyMetadata(true));

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      => ((ErrorIconControlV2)d).UpdateVisual(animate: true);

    private void UpdateVisual(bool animate)
    {
      if (Cross1 == null || Cross2 == null) return;

      if (!IsActive)
      {
        if (animate && UseAnimation)
        {
          BeginDash(Cross1, toOffset: 200, durationMs: 120, beginMs: 0);
          BeginDash(Cross2, toOffset: 200, durationMs: 120, beginMs: 60);
        }
        else
        {
          Cross1.StrokeDashOffset = 200;
          Cross2.StrokeDashOffset = 200;
        }
        return;
      }

      // Показать крестик (рисуем по диагоналям последовательно)
      if (animate && UseAnimation)
      {
        BeginDash(Cross1, toOffset: 0, durationMs: 160, beginMs: 0, fromOffset: 200);
        BeginDash(Cross2, toOffset: 0, durationMs: 160, beginMs: 70, fromOffset: 200);
      }
      else
      {
        Cross1.StrokeDashOffset = 0;
        Cross2.StrokeDashOffset = 0;
      }
    }

    private static void BeginDash(System.Windows.Shapes.Path path, double toOffset, int durationMs, int beginMs, double? fromOffset = null)
    {
      var anim = new DoubleAnimation
      {
        To = toOffset,
        Duration = TimeSpan.FromMilliseconds(durationMs),
        BeginTime = TimeSpan.FromMilliseconds(beginMs),
        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
      };
      if (fromOffset.HasValue) anim.From = fromOffset.Value;

      path.BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty, anim);
    }
  }
}
