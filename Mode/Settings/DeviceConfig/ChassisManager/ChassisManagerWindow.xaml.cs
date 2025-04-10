using System.Windows;
using DataBaseConfiguration.Models.Device;
using DataBaseConfiguration.Services;
using Mode.Settings.DeviceConfig.Base;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.ChassisManager
{
  /// <summary>
  /// Логика взаимодействия для ChassisManagerWindow.xaml.
  /// </summary>
  public partial class ChassisManagerWindow : Window, IDataProcessor
  {
    /// <summary>
    /// Событие запроса закрытия окна.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие запроса сохранения данных устройства.
    /// </summary>
    public event EventHandler<ChassisManagerEntity> RequestSave;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ChassisManagerWindow"/>.
    /// </summary>
    public ChassisManagerWindow()
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
    /// Устанавливает настройки для теста АСКМ.
    /// </summary>
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
            additionalDataProcessor: this);

        if (deviceEntity != null)
        {
          new ChassisManagerServices().Create(deviceEntity);
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
