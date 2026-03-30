using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Controls.Settings.DeviceConfig.ChassisManager
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
    public ObservableCollection<RackDto> SystemsRack { get; set; } = new();

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
    public RackDto SelectedRack
    {
      get => (RackDto)GetValue(SelectedRackProperty);
      set => SetValue(SelectedRackProperty, value);
    }

    /// <summary>
    /// Свойство зависимости для выбранной стойки.
    /// </summary>
    public static readonly DependencyProperty SelectedRackProperty =
        DependencyProperty.Register(
            nameof(SelectedRack),
            typeof(RackDto),
            typeof(ChassisManagerControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Событие, вызываемое при выборе системы.
    /// </summary>
    public event EventHandler<IChassisManager> SystemSelected;

    /// <summary>
    /// Событие, вызываемое при выборе стойки.
    /// </summary>
    public event EventHandler<RackDto> RackSelected;

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
    public void AddRack(RackDto rack)
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
    /// Сбрасывает локальное состояние отображения шасси и стоек.
    /// </summary>
    public void Reset()
    {
      SystemsChassis.Clear();
      SystemsRack.Clear();

      SelectedSystem = null;
      SelectedRack = null;

      addChassisButton.Visibility = Visibility.Visible;
      addRackButton.Visibility = Visibility.Collapsed;
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
      if (sender is Button button && button.DataContext is RackDto system)
      {
        SelectedRack = system;
        RackSelected?.Invoke(this, system);
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
