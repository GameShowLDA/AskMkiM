using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Представляет кнопку со стрелкой, которая может изменять направление в зависимости от состояния.
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
    /// Переопределяет метод для применения шаблона и инициализации поворота стрелки.
    /// </summary>
    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      _rootRotate = this.RenderTransform as RotateTransform;

      RotateArrow(IsArrowUp);
    }

    /// <summary>
    /// Реализует зависимость для свойства <see cref="ArrowColor"/> — цвет стрелки.
    /// </summary>
    public static readonly DependencyProperty ArrowColorProperty =
        DependencyProperty.Register(nameof(ArrowColor), typeof(Brush), typeof(ArrowButton),
            new PropertyMetadata(Brushes.White));

    /// <summary>
    /// Получает или задает цвет стрелки.
    /// </summary>
    /// <value>Цвет стрелки.</value>
    public Brush ArrowColor
    {
      get => (Brush)GetValue(ArrowColorProperty);
      set => SetValue(ArrowColorProperty, value);
    }

    /// <summary>
    /// Реализует зависимость для свойства <see cref="HoverArrowColor"/> — цвет стрелки при наведении.
    /// </summary>
    public static readonly DependencyProperty HoverArrowColorProperty =
        DependencyProperty.Register(nameof(HoverArrowColor), typeof(Brush), typeof(ArrowButton),
            new PropertyMetadata(Brushes.Gray));

    /// <summary>
    /// Получает или задает цвет стрелки при наведении.
    /// </summary>
    /// <value>Цвет стрелки при наведении.</value>
    public Brush HoverArrowColor
    {
      get => (Brush)GetValue(HoverArrowColorProperty);
      set => SetValue(HoverArrowColorProperty, value);
    }

    /// <summary>
    /// Реализует зависимость для свойства <see cref="IsArrowUp"/> — направление стрелки (вверх или вниз).
    /// </summary>
    public static readonly DependencyProperty IsArrowUpProperty =
        DependencyProperty.Register(nameof(IsArrowUp), typeof(bool), typeof(ArrowButton),
            new PropertyMetadata(false, OnIsArrowUpChanged));

    /// <summary>
    /// Получает или задает состояние стрелки: вверх (true) или вниз (false).
    /// </summary>
    /// <value><c>true</c>, если стрелка направлена вверх; <c>false</c>, если вниз.</value>
    public bool IsArrowUp
    {
      get => (bool)GetValue(IsArrowUpProperty);
      set => SetValue(IsArrowUpProperty, value);
    }

    private static void OnIsArrowUpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ArrowButton control)
      {
        control.RotateArrow((bool)e.NewValue);
      }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      IsArrowUp = !IsArrowUp;
      RotateArrow(IsArrowUp);
    }

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
