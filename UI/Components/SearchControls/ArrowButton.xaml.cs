using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Кастомный класс для работы со стрелкой, реализующий анимацию поворота при изменении состояния.
  /// </summary>
  public partial class ArrowButton : UserControl
  {
    private RotateTransform _rootRotate;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ArrowButton"/>.
    /// </summary>
    public ArrowButton()
    {
      InitializeComponent();
      this.DataContext = this;
    }

    /// <summary>
    /// Вызывается после применения шаблона и инициализирует трансформацию для анимации поворота.
    /// </summary>
    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      _rootRotate = this.RenderTransform as RotateTransform;

      RotateArrow(IsArrowUp);
    }

    /// <summary>
    /// Получает или устанавливает цвет стрелки.
    /// </summary>
    public static readonly DependencyProperty ArrowColorProperty =
        DependencyProperty.Register(nameof(ArrowColor), typeof(Brush), typeof(ArrowButton),
            new PropertyMetadata(Brushes.White));

    /// <summary>
    /// Получает или устанавливает цвет стрелки.
    /// </summary>
    public Brush ArrowColor
    {
      get => (Brush)GetValue(ArrowColorProperty);
      set => SetValue(ArrowColorProperty, value);
    }

    /// <summary>
    /// Получает или устанавливает цвет стрелки при наведении курсора.
    /// </summary>
    public static readonly DependencyProperty HoverArrowColorProperty =
        DependencyProperty.Register(nameof(HoverArrowColor), typeof(Brush), typeof(ArrowButton),
            new PropertyMetadata(Brushes.Gray));

    /// <summary>
    /// Получает или устанавливает цвет стрелки при наведении курсора.
    /// </summary>
    public Brush HoverArrowColor
    {
      get => (Brush)GetValue(HoverArrowColorProperty);
      set => SetValue(HoverArrowColorProperty, value);
    }

    /// <summary>
    /// Получает или устанавливает состояние, определяющее направление стрелки (вверх или вниз).
    /// </summary>
    public static readonly DependencyProperty IsArrowUpProperty =
        DependencyProperty.Register(nameof(IsArrowUp), typeof(bool), typeof(ArrowButton),
            new PropertyMetadata(false, OnIsArrowUpChanged));

    /// <summary>
    /// Получает или устанавливает состояние, определяющее направление стрелки (вверх или вниз).
    /// </summary>
    public bool IsArrowUp
    {
      get => (bool)GetValue(IsArrowUpProperty);
      set => SetValue(IsArrowUpProperty, value);
    }

    /// <summary>
    /// Обработчик изменения свойства IsArrowUp, вызывающий анимацию поворота стрелки.
    /// </summary>
    private static void OnIsArrowUpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ArrowButton control)
      {
        control.RotateArrow((bool)e.NewValue);
      }
    }

    /// <summary>
    /// Обрабатывает событие клика, переключая состояние IsArrowUp и инициируя анимацию поворота.
    /// </summary>
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      IsArrowUp = !IsArrowUp;
      RotateArrow(IsArrowUp);
    }

    /// <summary>
    /// Выполняет анимацию поворота стрелки в зависимости от значения IsArrowUp.
    /// </summary>
    private void RotateArrow(bool isUp)
    {
      if (_rootRotate == null)
      {
        return;
      }

      double targetAngle = isUp ? 180 : 0;

      DoubleAnimation animation = new DoubleAnimation(targetAngle, TimeSpan.FromMilliseconds(200))
      {
        EasingFunction = new QuadraticEase(),
      };
      _rootRotate.BeginAnimation(RotateTransform.AngleProperty, animation);
    }
  }
}
