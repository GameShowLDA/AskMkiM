using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using DataBaseConfiguration.Services.Device;
using System.Windows;
using UI.Controls.Settings.DeviceConfig.Base;
using UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;

namespace UI.Controls.Settings.DeviceConfig.ModuleRelayControl
{
  /// <summary>
  /// Логика взаимодействия для ModuleRelayControlWindow.xaml.
  /// </summary>
  public partial class ModuleRelayControlWindow : Window, IDataProcessor
  {
    /// <summary>
    /// Событие, вызываемое при закрытии окна.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие, вызываемое при сохранении данных модуля источника питания.
    /// </summary>
    public event EventHandler<RelaySwitchModuleEntity> RequestSave;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ModuleRelayControlWindow"/>.
    /// </summary>
    public ModuleRelayControlWindow()
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
    /// Устанавливает настройки для модуля коммутации реле.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Экземпляр головного устройства.</param>
    public void SetSettings(object? sender, IHeadUnit e)
    {
      deviceSettingsWindow.NameDevice = "МКР";
      deviceSettingsWindow.LoadDeviceModels<IRelaySwitchModule>();
      deviceSettingsWindow.SetHeadUnit(e);

      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        RelaySwitchModuleEntity deviceEntity = processor.ProcessDevice<RelaySwitchModuleEntity>(
            selectedDevice: baseDevice as IDevice,
            control: deviceSettingsWindow,
            additionalDataProcessor: this);

        if (deviceEntity != null)
        {
          deviceEntity.PointCount = (baseDevice as IRelaySwitchModule).PointCount;
          deviceEntity.BusType = (SwitchingBusNew)deviceSettingsWindow.BusTypeSelectionBox.SelectedItem;
          deviceEntity.SwitchResistance = deviceSettingsWindow.GetResistance();
          try
          {
            new RelaySwitchModuleServices().Create(deviceEntity);
            RequestSave?.Invoke(s, deviceEntity);
            Close();
          }
          catch (DuplicateEntityException ex)
          {
            var messsage = ex.Message;
            Message.MessageBoxCustom.Show(messsage, "Ошибка сохраненения данных", image: MessageBoxImage.Error);
            throw;
          }
        }
      };

      deviceSettingsWindow.RequestClose += (s, a) =>
      {
        RequestClose?.Invoke(s, a);
        Close();
      };
    }
  }
}
