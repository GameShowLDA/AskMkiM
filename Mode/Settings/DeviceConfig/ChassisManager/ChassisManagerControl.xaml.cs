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
    public ObservableCollection<ChassisManagerEntity> Systems { get; set; } = new();

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
    /// Событие, вызываемое при выборе системы.
    /// </summary>
    public event EventHandler<ChassisManagerEntity> SystemSelected;
    public event EventHandler NewSystem;

    public ChassisManagerControl()
    {
      InitializeComponent();
      DataContext = this;
    }

    /// <summary>
    /// Добавляет систему в список для отображения.
    /// </summary>
    /// <param name="chassisManager">Экземпляр ChassisManagerEntity</param>
    public void AddSystem(ChassisManagerEntity chassisManager)
    {
      if (chassisManager == null)
        return;

      Systems.Add(chassisManager);
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

    private void addChassisButton_MouseEnter(object sender, MouseEventArgs e)
    {
      addChassisButton.Background = (Brush)Application.Current.Resources["IsCheckedColorSolidColorBrush"];
      addChassisButton.Cursor = Cursors.Hand;
    }

    private void addChassisButton_MouseLeave(object sender, MouseEventArgs e)
    {
      addChassisButton.Background = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
      addChassisButton.Cursor = Cursors.Wait;
    }

    private void addChassisButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      NewSystem?.Invoke(this, e);

    }
  }
}
