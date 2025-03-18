using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AppConfig.DataBase.Models;

namespace Mode.Settings.DeviceConfig.ChassisManager
{
  /// <summary>
  /// Логика взаимодействия для DeviceConfigControl.xaml
  /// </summary>
  public partial class ChassisManagerControl : UserControl
  {
    public ObservableCollection<ChassisManagerEntity> SystemsChassis { get; set; } = new();
    public ObservableCollection<RackEntity> SystemsRack { get; set; } = new();

    /// <summary>
    /// Выбранная система.
    /// </summary>
    public ChassisManagerEntity SelectedSystem
    {
      get { return (ChassisManagerEntity)GetValue(SelectedSystemProperty); }
      set { SetValue(SelectedSystemProperty, value); }
    }

    public static readonly DependencyProperty SelectedSystemProperty =
        DependencyProperty.Register(nameof(SelectedSystem), typeof(ChassisManagerEntity), typeof(ChassisManagerControl), new PropertyMetadata(null));


    /// <summary>
    /// Выбранная система.
    /// </summary>
    public RackEntity SelectedRack
    {
      get { return (RackEntity)GetValue(SelectedRackProperty); }
      set { SetValue(SelectedRackProperty, value); }
    }

    public static readonly DependencyProperty SelectedRackProperty =
       DependencyProperty.Register(nameof(SelectedRack), typeof(RackEntity), typeof(ChassisManagerControl), new PropertyMetadata(null));


    /// <summary>
    /// Событие, вызываемое при выборе системы.
    /// </summary>
    public event EventHandler<ChassisManagerEntity> SystemSelected;
    public event EventHandler<RackEntity> RackSelected;

    public event EventHandler NewSystem;
    public event EventHandler NewRack;

    public ChassisManagerControl()
    {
      InitializeComponent();
      DataContext = this;
      addRackButton.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Добавляет систему в список для отображения.
    /// </summary>
    /// <param name="chassisManager">Экземпляр ChassisManagerEntity</param>
    public void AddSystem(ChassisManagerEntity chassisManager)
    {
      if (chassisManager == null)
        return;

      SystemsChassis.Add(chassisManager);
      addChassisButton.Visibility = Visibility.Collapsed;
      addRackButton.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Добавляет систему в список для отображения.
    /// </summary>
    /// <param name="chassisManager">Экземпляр ChassisManagerEntity</param>
    public void AddRack(RackEntity rack)
    {
      if (rack == null)
        return;

      SystemsRack.Add(rack);
      addChassisButton.Visibility = Visibility.Collapsed;
      addRackButton.Visibility = Visibility.Visible;
    }


    /// <summary>
    /// Вызывается при нажатии кнопки.
    /// Устанавливает выбранную систему и вызывает событие.
    /// </summary>
    private void OnSystemSelected(object sender, RoutedEventArgs e)
    {
      if (sender is Button button && button.DataContext is ChassisManagerEntity system)
      {
        SelectedSystem = system;
        SystemSelected?.Invoke(this, system);
      }
    }

    /// <summary>
    /// Вызывается при нажатии кнопки.
    /// Устанавливает выбранную систему и вызывает событие.
    /// </summary>
    private void OnRackSelected(object sender, RoutedEventArgs e)
    {
      if (sender is Button button && button.DataContext is RackEntity system)
      {
        SelectedRack = system;
        RackSelected?.Invoke(this, system);
      }
    }

    private void addChassisButton_MouseEnter(object sender, MouseEventArgs e)
    {
      (sender as Border).Background = (Brush)Application.Current.Resources["IsCheckedColorSolidColorBrush"];
      (sender as Border).Cursor = Cursors.Hand;
    }

    private void addChassisButton_MouseLeave(object sender, MouseEventArgs e)
    {
      (sender as Border).Background = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
      (sender as Border).Cursor = Cursors.Wait;
    }

    private void addChassisButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      NewSystem?.Invoke(this, e);
    }

    private void addRackButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      NewRack?.Invoke(this, e);
    }
  }
}
