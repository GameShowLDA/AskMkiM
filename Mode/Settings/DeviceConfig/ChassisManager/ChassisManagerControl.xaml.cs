using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppConfig.DataBase.Models;

namespace Mode.Settings.DeviceConfig.ChassisManager
{
  /// <summary>
  /// Логика взаимодействия для DeviceConfigControl.xaml
  /// </summary>
  public partial class ChassisManagerControl : UserControl
  {
    public ObservableCollection<ChassisManagerEntity> Systems { get; set; } = new();

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
  }
}
