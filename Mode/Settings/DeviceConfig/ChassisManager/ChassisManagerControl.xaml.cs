using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AppConfig.DataBase.Models;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.ChassisManager
{
  /// <summary>
  /// Логика взаимодействия для DeviceConfigControl.xaml.
  /// </summary>
  public partial class ChassisManagerControl : UserControl
  {
    /// <summary>
    /// Коллекция доступных систем шасси.
    /// </summary>
    public ObservableCollection<IChassisManager> SystemsChassis { get; set; } = new();

    /// <summary>
    /// Коллекция доступных стоек.
    /// </summary>
    public ObservableCollection<RackEntity> SystemsRack { get; set; } = new();

    /// <summary>
    /// Выбранная система шасси.
    /// </summary>
    public IChassisManager SelectedSystem
    {
      get => (IChassisManager)GetValue(SelectedSystemProperty);
      set => SetValue(SelectedSystemProperty, value);
    }

    /// <summary>
    /// Свойство зависимости для выбранной системы шасси.
    /// </summary>
    public static readonly DependencyProperty SelectedSystemProperty =
        DependencyProperty.Register(
            nameof(SelectedSystem),
            typeof(IChassisManager),
            typeof(ChassisManagerControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Выбранная стойка.
    /// </summary>
    public RackEntity SelectedRack
    {
      get => (RackEntity)GetValue(SelectedRackProperty);
      set => SetValue(SelectedRackProperty, value);
    }

    /// <summary>
    /// Свойство зависимости для выбранной стойки.
    /// </summary>
    public static readonly DependencyProperty SelectedRackProperty =
        DependencyProperty.Register(
            nameof(SelectedRack),
            typeof(RackEntity),
            typeof(ChassisManagerControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Событие, вызываемое при выборе системы.
    /// </summary>
    public event EventHandler<IChassisManager> SystemSelected;

    /// <summary>
    /// Событие, вызываемое при выборе стойки.
    /// </summary>
    public event EventHandler<RackEntity> RackSelected;

    /// <summary>
    /// Событие, вызываемое при добавлении новой системы.
    /// </summary>
    public event EventHandler NewSystem;

    /// <summary>
    /// Событие, вызываемое при добавлении новой стойки.
    /// </summary>
    public event EventHandler NewRack;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ChassisManagerControl"/>.
    /// </summary>
    public ChassisManagerControl()
    {
      InitializeComponent();
      DataContext = this;
      addRackButton.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Добавляет систему в список для отображения.
    /// </summary>
    /// <param name="chassisManager">Экземпляр <see cref="ChassisManagerEntity"/>.</param>
    public void AddSystem(IChassisManager chassisManager)
    {
      if (chassisManager == null)
      {
        return;
      }

      SystemsChassis.Add(chassisManager);
      addChassisButton.Visibility = Visibility.Collapsed;
      addRackButton.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Добавляет стойку в список для отображения.
    /// </summary>
    /// <param name="rack">Экземпляр <see cref="RackEntity"/>.</param>
    public void AddRack(RackEntity rack)
    {
      if (rack == null)
      {
        return;
      }

      SystemsRack.Add(rack);
      addChassisButton.Visibility = Visibility.Collapsed;
      addRackButton.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Обрабатывает выбор системы.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OnSystemSelected(object sender, RoutedEventArgs e)
    {
      if (sender is Button button && button.DataContext is IChassisManager system)
      {
        SelectedSystem = system;
        SystemSelected?.Invoke(this, system);
      }
    }

    /// <summary>
    /// Обрабатывает выбор стойки.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OnRackSelected(object sender, RoutedEventArgs e)
    {
      if (sender is Button button && button.DataContext is RackEntity system)
      {
        SelectedRack = system;
        RackSelected?.Invoke(this, system);
      }
    }

    /// <summary>
    /// Обрабатывает наведение курсора на кнопку добавления системы.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void addChassisButton_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is Border border)
      {
        border.Background = (Brush)Application.Current.Resources["IsCheckedColorSolidColorBrush"];
        border.Cursor = Cursors.Hand;
      }
    }

    /// <summary>
    /// Обрабатывает уход курсора с кнопки добавления системы.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void addChassisButton_MouseLeave(object sender, MouseEventArgs e)
    {
      if (sender is Border border)
      {
        border.Background = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
        border.Cursor = Cursors.Wait;
      }
    }

    /// <summary>
    /// Обрабатывает нажатие на кнопку добавления системы.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void addChassisButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      NewSystem?.Invoke(this, e);
    }

    /// <summary>
    /// Обрабатывает нажатие на кнопку добавления стойки.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void addRackButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      NewRack?.Invoke(this, e);
    }
  }
}
