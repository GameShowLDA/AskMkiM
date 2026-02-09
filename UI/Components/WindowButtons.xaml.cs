using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для WindowButtons.xaml.
  /// </summary>
  public partial class WindowButtons : UserControl
  {
    public static readonly DependencyProperty CommandProperty =
       DependencyProperty.Register(
           nameof(Command),
           typeof(ICommand),
           typeof(WindowButtons),
           new PropertyMetadata(null));

    /// <summary>
    /// Команда, вызываемая при нажатии на кнопку.
    /// </summary>
    public ICommand Command
    {
      get => (ICommand)GetValue(CommandProperty);
      set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
      DependencyProperty.Register(
        nameof(CommandParameter),
        typeof(object),
        typeof(WindowButtons),
        new PropertyMetadata(null));

    public object CommandParameter
    {
      get => GetValue(CommandParameterProperty);
      set => SetValue(CommandParameterProperty, value);
    }

    public static readonly DependencyProperty CommandTargetProperty =
      DependencyProperty.Register(
        nameof(CommandTarget),
        typeof(IInputElement),
        typeof(WindowButtons),
        new PropertyMetadata(null));

    public IInputElement CommandTarget
    {
      get => (IInputElement)GetValue(CommandTargetProperty);
      set => SetValue(CommandTargetProperty, value);
    }

    /// <summary>
    /// Перечисление, представляющее варианты выбора для кнопок окна.
    /// </summary>
    public enum Choice
    {
      Exit,
      Minimize,
      Maximize,
      None,
    }

    /// <summary>
    /// Свойство, определяющее текущий выбранный элемент кнопки окна.
    /// </summary>
    public Choice ChoiceElement { get; set; }

    /// <summary>
    /// Переопределенный метод для отрисовки содержимого элемента управления.
    /// </summary>
    /// <param name="dc">Контекст отрисовки.</param>
    protected override void OnRender(DrawingContext dc)
    {
      base.OnRender(dc);

      if (ActualWidth <= 0 || ActualHeight <= 0)
      {
        return;
      }

      Pen pen = new Pen(Foreground, 1);

      double widthRect = ActualWidth / 4;
      double heightRect = ActualHeight / 4;
      double locationX = (ActualWidth - widthRect) / 2;
      double locationY = (ActualHeight - heightRect) / 2;

      if (ChoiceElement == Choice.Exit)
      {
        dc.DrawLine(pen, new Point(locationX, locationY), new Point(widthRect + locationX, heightRect + locationY));
        dc.DrawLine(pen, new Point(widthRect + locationX, locationY), new Point(locationX, heightRect + locationY));
      }
      else if (ChoiceElement == Choice.Minimize)
      {
        dc.DrawLine(pen, new Point(locationX, ActualWidth / 2), new Point(locationX + widthRect, ActualWidth / 2));
      }
      else if (ChoiceElement == Choice.Maximize)
      {
        dc.DrawRectangle(null, pen, new Rect(locationX, locationY, widthRect, heightRect));
      }
    }

    /// <summary>
    /// Переопределенный метод, вызываемый при изменении размера элемента управления.
    /// </summary>
    /// <param name="sizeInfo">Информация об изменении размера.</param>
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
      base.OnRenderSizeChanged(sizeInfo);
      Width = Height = sizeInfo.NewSize.Width < sizeInfo.NewSize.Height ? sizeInfo.NewSize.Width : sizeInfo.NewSize.Height;
      InvalidateVisual();
    }

    /// <summary>
    /// Конструктор класса WindowButtons.
    /// </summary>
    public WindowButtons()
    {
      InitializeComponent();
      ChoiceElement = Choice.None;
      Width = Height = 20;
      Cursor = Cursors.Hand;
      this.PreviewMouseDown += WindowButtons_PreviewMouseDown;
    }

    private void WindowButtons_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (Command == null)
        return;

      if (TryExecuteSystemCommand())
        return;

      ExecuteCommand(Command, CommandParameter, CommandTarget);
    }

    private bool TryExecuteSystemCommand()
    {
      if (!IsSystemCommand(Command))
        return false;

      if (CommandParameter != null || CommandTarget != null)
        return false;

      Window window = Window.GetWindow(this);
      if (window == null)
        return false;

      if (ReferenceEquals(Command, SystemCommands.MinimizeWindowCommand))
      {
        SystemCommands.MinimizeWindow(window);
        return true;
      }

      if (ReferenceEquals(Command, SystemCommands.MaximizeWindowCommand))
      {
        if (window.WindowState == WindowState.Maximized)
          SystemCommands.RestoreWindow(window);
        else
          SystemCommands.MaximizeWindow(window);
        return true;
      }

      if (ReferenceEquals(Command, SystemCommands.RestoreWindowCommand))
      {
        SystemCommands.RestoreWindow(window);
        return true;
      }

      if (ReferenceEquals(Command, SystemCommands.CloseWindowCommand))
      {
        SystemCommands.CloseWindow(window);
        return true;
      }

      return false;
    }

    private void ExecuteCommand(ICommand command, object commandParameter, IInputElement commandTarget)
    {
      if (IsSystemCommand(command))
      {
        Window window = Window.GetWindow(this);
        if (commandParameter == null)
          commandParameter = window;
        if (commandTarget == null)
          commandTarget = window;
      }

      if (command is RoutedCommand routedCommand)
      {
        IInputElement target = commandTarget ?? this;
        if (routedCommand.CanExecute(commandParameter, target))
          routedCommand.Execute(commandParameter, target);
        return;
      }

      if (command.CanExecute(commandParameter))
        command.Execute(commandParameter);
    }

    private static bool IsSystemCommand(ICommand command)
    {
      return ReferenceEquals(command, SystemCommands.MinimizeWindowCommand)
        || ReferenceEquals(command, SystemCommands.MaximizeWindowCommand)
        || ReferenceEquals(command, SystemCommands.RestoreWindowCommand)
        || ReferenceEquals(command, SystemCommands.CloseWindowCommand);
    }

    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(nameof(Size), typeof(double), typeof(WindowButtons),
          new PropertyMetadata(64.0, OnSizeChanged));

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is WindowButtons icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }

  }
}
