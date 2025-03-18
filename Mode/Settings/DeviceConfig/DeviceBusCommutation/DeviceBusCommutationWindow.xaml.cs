using System.Windows;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using Mode.Settings.DeviceConfig.Base;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base.Device;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.DeviceBusCommutation
{
  /// <summary>
  /// Логика взаимодействия для DeviceBusCommutationWindow.xaml
  /// </summary>
  public partial class DeviceBusCommutationWindow : Window, IDataProcessor
  {
    public EventHandler RequestClose;
    public EventHandler<SwitchingDeviceEntity> RequestSave;
    public DeviceBusCommutationWindow()
    {
      InitializeComponent();
    }

    public DeviceBase Property => new DeviceBase(deviceSettingsWindow);

    public void ProcessData(IDevice device, DeviceSettingsControl control)
    {
      return;
    }

    public void SetSettings(object? sender, IHeadUnit e)
    {
      deviceSettingsWindow.NameDevice = "Устройство коммутации шин";
      deviceSettingsWindow.LoadDeviceModels<ISwitchingDevice>();
      deviceSettingsWindow.SetHeadUnit(e);

      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        SwitchingDeviceEntity deviceEntity = processor.ProcessDevice<SwitchingDeviceEntity>(
          selectedDevice: baseDevice as IDevice,
          control: deviceSettingsWindow,
          additionalDataProcessor: this
          );

        if (deviceEntity != null)
        {
          new SwitchingDeviceRepository(AppConfig.Config.SystemStateManager.Context).Create(deviceEntity);
        }

        RequestSave?.Invoke(s, deviceEntity as SwitchingDeviceEntity);

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
