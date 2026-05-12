using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.DataBase.Engine.Static.Devices;
using System.Threading.Tasks;
using System.Windows;
using UI.Controls.Settings.DeviceConfig.Base;
using UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;
using static UI.Controls.Settings.DeviceConfig.DeviceConfigNotifications;

namespace UI.Controls.Settings.DeviceConfig.ModuleRelayControl
{
  /// <summary>
  /// Логика взаимодействия для ModuleRelayControlWindow.xaml.
  /// </summary>
  public partial class ModuleRelayControlWindow : Window, IDataProcessor
  {
    public Action? CloseActionOverride { get; set; }
    private RelaySwitchModuleDto? _editingDto;

    /// <summary>
    /// Событие, вызываемое при закрытии окна.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие, вызываемое при сохранении данных модуля источника питания.
    /// </summary>
    public event EventHandler<RelaySwitchModuleDto> RequestSave;

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

    public DeviceSettingsControl DetachSettingsControl()
    {
      Content = null;
      return deviceSettingsWindow;
    }

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
    public void SetSettings(object? sender, IHeadUnit e, RelaySwitchModuleDto? editingEntity = null)
    {
      _editingDto = editingEntity;
      deviceSettingsWindow.NameDevice = "МКР";
      deviceSettingsWindow.LoadDeviceModels<IRelaySwitchModule>();
      deviceSettingsWindow.SetHeadUnit(e);
      if (editingEntity != null)
      {
        deviceSettingsWindow.LoadFromDevice(editingEntity);
      }

      deviceSettingsWindow.SaveEvent += async (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        RelaySwitchModuleDto deviceEntity = processor.ProcessDevice<RelaySwitchModuleDto>(
            selectedDevice: baseDevice as IDevice,
            control: deviceSettingsWindow,
            additionalDataProcessor: this);

        if (deviceEntity != null)
        {
          deviceEntity.PointCount = (baseDevice as IRelaySwitchModule).PointCount;
          deviceEntity.BusType = (SwitchingBusNew)deviceSettingsWindow.BusTypeSelectionBox.SelectedItem;
          deviceEntity.SwitchResistance = deviceSettingsWindow.GetResistance();
          deviceEntity.SwitchCapacitance = deviceSettingsWindow.GetCapacitance();
          try
          {
            if (_editingDto != null)
            {
              deviceEntity.Id = _editingDto.Id;
            }

            var relaySwitchModule = RelaySwitchModules.Build(deviceEntity);

            if (_editingDto == null)
            {
              var createdDevice = await RelaySwitchModules.CreateAsync(relaySwitchModule);
              deviceEntity.Id = createdDevice.Id;
              ShowCreated(deviceEntity);
            }
            else
            {
              await RelaySwitchModules.UpdateAsync(relaySwitchModule);
              ShowUpdated(deviceEntity);
            }

            RequestCloseWindow();
            RequestSave?.Invoke(s, deviceEntity);
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
        RequestCloseWindow();
      };
    }

    private void RequestCloseWindow()
    {
      if (CloseActionOverride != null)
      {
        CloseActionOverride.Invoke();
        return;
      }

      Close();
    }
  }
}
