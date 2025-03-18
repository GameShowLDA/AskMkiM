using System.Windows;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using Mode.Settings.DeviceConfig.Base;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base.Device;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.FastMeter
{
  /// <summary>
  /// Логика взаимодействия для FastMeterWindow.xaml
  /// </summary>
  public partial class FastMeterWindow : Window, IDataProcessor
  {
    public EventHandler RequestClose;
    public EventHandler<FastMeterEntity> RequestSave;
    public FastMeterWindow()
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
      deviceSettingsWindow.NameDevice = "Измеритель (быстрый)";
      deviceSettingsWindow.LoadDeviceModels<IFastMeter>();
      deviceSettingsWindow.SetHeadUnit(e);

      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        FastMeterEntity deviceEntity = processor.ProcessDevice<FastMeterEntity>(
          selectedDevice: baseDevice as IDevice,
          control: deviceSettingsWindow,
          additionalDataProcessor: this
          );

        if (deviceEntity != null)
        {
          new FastMeterRepository(AppConfig.Config.SystemStateManager.Context).Create(deviceEntity);
        }

        RequestSave?.Invoke(s, deviceEntity as FastMeterEntity);

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
