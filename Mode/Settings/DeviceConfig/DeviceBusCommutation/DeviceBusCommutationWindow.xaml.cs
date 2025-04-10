using System.Windows;
using DataBaseConfiguration.Models.Device;
using DataBaseConfiguration.Services;
using Mode.Settings.DeviceConfig.Base;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base.Device;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.DeviceBusCommutation
{
  /// <summary>
  /// Логика взаимодействия для DeviceBusCommutationWindow.xaml.
  /// </summary>
  public partial class DeviceBusCommutationWindow : Window, IDataProcessor
  {
    /// <summary>
    /// Событие запроса закрытия окна.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие запроса сохранения данных устройства.
    /// </summary>
    public event EventHandler<SwitchingDeviceEntity> RequestSave;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceBusCommutationWindow"/>.
    /// </summary>
    public DeviceBusCommutationWindow()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Свойство, предоставляющее доступ к параметрам устройства.
    /// </summary>
    public DeviceBase Property => new DeviceBase(deviceSettingsWindow);

    /// <summary>
    /// Обрабатывает данные устройства.
    /// </summary>
    /// <param name="device">Экземпляр устройства.</param>
    /// <param name="control">Элемент управления настройками устройства.</param>
    public void ProcessData(IDevice device, DeviceSettingsControl control)
    {
      return;
    }

    /// <summary>
    /// Устанавливает настройки для устройства коммутации шин.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Экземпляр головного устройства.</param>
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
            additionalDataProcessor: this);

        if (deviceEntity != null)
        {
          new SwitchingDeviceServices().Create(deviceEntity);
        }

        RequestSave?.Invoke(s, deviceEntity);
        Close();
      };

      deviceSettingsWindow.RequestClose += (s, a) =>
      {
        RequestClose?.Invoke(s, a);
        Close();
      };
    }
  }
}
