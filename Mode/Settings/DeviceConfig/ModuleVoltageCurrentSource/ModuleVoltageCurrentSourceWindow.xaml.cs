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
using System.Windows.Shapes;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using Mode.Settings.DeviceConfig.Base;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base.Device;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Логика взаимодействия для ModuleVoltageCurrentSourceWindow.xaml
  /// </summary>
  public partial class ModuleVoltageCurrentSourceWindow : Window, IDataProcessor
  {
    public EventHandler RequestClose;
    public EventHandler<PowerSourceModuleEntity> RequestSave;
    public ModuleVoltageCurrentSourceWindow()
    {
      InitializeComponent();
    }

    public DeviceBase Property => new DeviceBase(deviceSettingsWindow);


    public void ProcessData(IDevice device, DeviceSettingsControl control)
    {
      throw new NotImplementedException();
    }

    public void SetSettings(object? sender, IHeadUnit e)
    {
      deviceSettingsWindow.NameDevice = "МИНТ";
      deviceSettingsWindow.LoadDeviceModels<IPowerSourceModule>();
      deviceSettingsWindow.SetHeadUnit(e);

      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        PowerSourceModuleEntity deviceEntity = processor.ProcessDevice<PowerSourceModuleEntity>(
          selectedDevice: baseDevice as IDevice,
          control: deviceSettingsWindow,
          additionalDataProcessor: this
          );

        if (deviceEntity != null)
        {
          new PowerSourceModuleRepository(AppConfig.Config.SystemStateManager.Context).Create(deviceEntity);
        }

        RequestSave?.Invoke(s, deviceEntity as PowerSourceModuleEntity);

        this.Close();
      };

      deviceSettingsWindow.RequestClose += (s, a) =>
      {
        RequestClose?.Invoke(s, a);
        this.Close();
      };
    }
  }
}
