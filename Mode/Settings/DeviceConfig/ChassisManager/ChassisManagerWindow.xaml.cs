using System.Windows;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using Mode.Settings.DeviceConfig.Base;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.ChassisManager
{
  /// <summary>
  /// Логика взаимодействия для ChassisManagerWindow.xaml
  /// </summary>
  public partial class ChassisManagerWindow : Window, IDataProcessor
  {
    public EventHandler RequestClose;
    public EventHandler<ChassisManagerEntity> RequestSave;
    public ChassisManagerWindow()
    {
      InitializeComponent();
    }

    public DeviceBase Property => new DeviceBase(deviceSettingsWindow);


    public void ProcessData(IDevice device, DeviceSettingsControl control)
    {
      return;
    }

    public void SetSettings()
    {
      deviceSettingsWindow.NameDevice = "Тест АСКМ";
      deviceSettingsWindow.LoadDeviceModels<IChassisManager>();

      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        ChassisManagerEntity deviceEntity = processor.ProcessDevice<ChassisManagerEntity>(
            selectedDevice: baseDevice as IDevice,
            control: deviceSettingsWindow,
            additionalDataProcessor: this
        );

        if (deviceEntity != null)
        {
          new ChassisManagerRepository(AppConfig.Config.SystemStateManager.Context).Create(deviceEntity);
        }

        RequestSave?.Invoke(s, deviceEntity as ChassisManagerEntity);
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
