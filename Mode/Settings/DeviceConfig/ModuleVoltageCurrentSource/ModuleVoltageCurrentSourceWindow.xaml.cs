using System.Windows;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Repositories;
using AppConfig.DataBase.Services;
using Mode.Settings.DeviceConfig.Base;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base.Device;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Логика взаимодействия для ModuleVoltageCurrentSourceWindow.xaml.
  /// </summary>
  public partial class ModuleVoltageCurrentSourceWindow : Window, IDataProcessor
  {
    /// <summary>
    /// Событие, вызываемое при закрытии окна.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие, вызываемое при сохранении данных модуля источника питания.
    /// </summary>
    public event EventHandler<PowerSourceModuleEntity> RequestSave;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ModuleVoltageCurrentSourceWindow"/>.
    /// </summary>
    public ModuleVoltageCurrentSourceWindow()
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
      throw new NotImplementedException();
    }

    /// <summary>
    /// Устанавливает настройки для модуля источника питания.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Экземпляр головного устройства.</param>
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
            additionalDataProcessor: this);

        if (deviceEntity != null)
        {
          new PowerSourceModuleServices().Create(deviceEntity);
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
