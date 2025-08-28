using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Shapes;

namespace UI.Icon
{
  /// <summary>Кнопка со стрелкой (вниз/вверх) с анимацией поворота.</summary>
  public partial class ArrowButton : UserControl
  {
    public ArrowButton()
    {
      InitializeComponent();
    }

    // ===== Публичные DependencyProperty для цветов =====

    /// <summary>Цвет фона плитки.</summary>
    public Brush ButtonBackground
    {
      get => (Brush)GetValue(ButtonBackgroundProperty);
      set => SetValue(ButtonBackgroundProperty, value);
    }
    public static readonly DependencyProperty ButtonBackgroundProperty =
      DependencyProperty.Register(nameof(ButtonBackground), typeof(Brush), typeof(ArrowButton),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x2E, 0x3B, 0x4E)))); // #2E3B4E

    /// <summary>Цвет границы плитки.</summary>
    public Brush ButtonBorder
    {
      get => (Brush)GetValue(ButtonBorderProperty);
      set => SetValue(ButtonBorderProperty, value);
    }
    public static readonly DependencyProperty ButtonBorderProperty =
      DependencyProperty.Register(nameof(ButtonBorder), typeof(Brush), typeof(ArrowButton),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x4A, 0x5C, 0x72)))); // #4A5C72

    /// <summary>Цвет стрелки в обычном состоянии.</summary>
    public Brush ArrowBrush
    {
      get => (Brush)GetValue(ArrowBrushProperty);
      set => SetValue(ArrowBrushProperty, value);
    }
    public static readonly DependencyProperty ArrowBrushProperty =
      DependencyProperty.Register(nameof(ArrowBrush), typeof(Brush), typeof(ArrowButton),
        new PropertyMetadata(Brushes.Black));

    /// <summary>Цвет стрелки при наведении.</summary>
    public Brush ArrowHoverBrush
    {
      get => (Brush)GetValue(ArrowHoverBrushProperty);
      set => SetValue(ArrowHoverBrushProperty, value);
    }
    public static readonly DependencyProperty ArrowHoverBrushProperty =
      DependencyProperty.Register(nameof(ArrowHoverBrush), typeof(Brush), typeof(ArrowButton),
        new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0xFF, 0x22, 0x22, 0x22))));

    // ===== Направление стрелки =====

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
      var ctrl = (ArrowButton)d;
    }

    /// <summary>
    /// Обрабатывает событие клика, переключая состояние IsArrowUp и инициируя анимацию поворота.
    /// </summary>
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      IsArrowUp = !IsArrowUp;
    }
  }
}
