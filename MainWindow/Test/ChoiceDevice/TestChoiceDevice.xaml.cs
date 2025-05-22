using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataBaseConfiguration.Services.Device;

namespace MainWindowProgram.Test.ChoiceDevice
{
  /// <summary>
  /// Логика взаимодействия для TestChoiceDevice.xaml
  /// </summary>
  public partial class TestChoiceDevice : UserControl
  {
    public TestChoiceDevice()
    {
      InitializeComponent();
      LoadDevicesForTop();
      LoadDevicesForBottom();
      LoadDevicesForMeter();
    }

    private void LoadDevicesForTop()
    {
      var chassisManagers = new ChassisManagerServices().GetAll();
      var names = new List<string>();
      foreach (var chassisManager in chassisManagers)
      { 
        names.Add(chassisManager.Name + " " + chassisManager.Number);
      }

      choiceDeviceTop.ItemsSource = chassisManagers;
      choiceDeviceTop.DisplayFields = names;
    }

    private void LoadDevicesForBottom()
    {
      var chassisManagers = new RelaySwitchModuleServices().GetAll();
      var names = new List<string>();
      foreach (var chassisManager in chassisManagers)
      {
        names.Add(chassisManager.Name + " (" + chassisManager.NumberChassis + "." + chassisManager.Number + ")");
      }

      choiceDeviceBottom.ItemsSource = chassisManagers;
      choiceDeviceBottom.DisplayFields = names;
    }

    private void LoadDevicesForMeter()
    {
      var chassisManagers = new FastMeterServices().GetAll();
      var names = new List<string>();
      foreach (var chassisManager in chassisManagers)
      {
        names.Add(chassisManager.Name + " (" + chassisManager.NumberChassis + "." + chassisManager.Number + ")");
      }

      choiceDeviceMeter.ItemsSource = chassisManagers;
      choiceDeviceMeter.DisplayFields = names;
    }
  }
}
