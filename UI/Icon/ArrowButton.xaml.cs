using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;

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

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      // 1) Сначала пробрасываем наружу "глобальный" Click
      RaiseEvent(new RoutedEventArgs(ClickEvent, this));

      // 2) Выполняем команду (если задана)
      if (Command?.CanExecute(CommandParameter) == true)
        Command.Execute(CommandParameter);

      // 3) Переключаем визуальное состояние (если включено)
      if (ToggleOnClick)
        IsArrowUp = !IsArrowUp;
    }

    // ======= ICommand поддержка =======
    public static readonly DependencyProperty CommandProperty =
      DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ArrowButton), new PropertyMetadata(null));

    public ICommand? Command
    {
      get => (ICommand?)GetValue(CommandProperty);
      set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
      DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(ArrowButton), new PropertyMetadata(null));

    public object? CommandParameter
    {
      get => GetValue(CommandParameterProperty);
      set => SetValue(CommandParameterProperty, value);
    }

    // ======= Поведение клика =======
    public static readonly DependencyProperty ToggleOnClickProperty =
      DependencyProperty.Register(nameof(ToggleOnClick), typeof(bool), typeof(ArrowButton),
        new PropertyMetadata(true));

    /// <summary>Авто-переключать IsArrowUp при клике.</summary>
    public bool ToggleOnClick
    {
      get => (bool)GetValue(ToggleOnClickProperty);
      set => SetValue(ToggleOnClickProperty, value);
    }

    // ======= Маршрутизируемое событие Click =======
    public static readonly RoutedEvent ClickEvent =
      EventManager.RegisterRoutedEvent(
        nameof(Click),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(ArrowButton));

    /// <summary>Событие клика, всплывающее вверх по дереву.</summary>
    public event RoutedEventHandler Click
    {
      add => AddHandler(ClickEvent, value);
      remove => RemoveHandler(ClickEvent, value);
    }
  }
}
