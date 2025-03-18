using System.Windows;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using Mode.Settings.DeviceConfig.Base;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base.Device;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.BreakDown
{
  /// <summary>
  /// Логика взаимодействия для BreakDownWindow.xaml
  /// </summary>
  public partial class BreakDownWindow : Window, IDataProcessor
  {
    public EventHandler RequestClose;
    public EventHandler<BreakdownTesterEntity> RequestSave;
    public DeviceBase Property => new DeviceBase(deviceSettingsWindow);

    public BreakDownWindow()
    {
      InitializeComponent();
    }

    public void ProcessData(IDevice device, DeviceSettingsControl control)
    {
      throw new NotImplementedException();
    }

    public void SetSettings(object? sender, IHeadUnit e)
    {
      deviceSettingsWindow.NameDevice = "Пробойная установка";
      deviceSettingsWindow.LoadDeviceModels<IBreakdownTester>();
      deviceSettingsWindow.SetHeadUnit(e);

      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        BreakdownTesterEntity deviceEntity = processor.ProcessDevice<BreakdownTesterEntity>(
          selectedDevice: baseDevice as IDevice,
          control: deviceSettingsWindow,
          additionalDataProcessor: this
          );

        if (deviceEntity != null)
        {
          new BreakdownTesterRepository(AppConfig.Config.SystemStateManager.Context).Create(deviceEntity);
        }

        RequestSave?.Invoke(s, deviceEntity);

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
