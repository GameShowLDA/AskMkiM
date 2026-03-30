using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.DataBase.Engine.Static.Devices;
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
    public Action? CloseActionOverride { get; set; }
    private RelaySwitchModuleEntity? _editingEntity;

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
    public void SetSettings(object? sender, IHeadUnit e, RelaySwitchModuleEntity? editingEntity = null)
    {
      _editingEntity = editingEntity;
      deviceSettingsWindow.NameDevice = "МКР";
      deviceSettingsWindow.LoadDeviceModels<IRelaySwitchModule>();
      deviceSettingsWindow.SetHeadUnit(e);
      if (editingEntity != null)
      {
        deviceSettingsWindow.LoadFromDevice(editingEntity);
      }

      deviceSettingsWindow.SaveEvent += (s, a) =>
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
            var relaySwitchModule = RelaySwitchModules.Build(deviceEntity);

            if (_editingEntity == null)
            {
              var createdDevice = RelaySwitchModules.CreateAsync(relaySwitchModule).GetAwaiter().GetResult();
              deviceEntity.Id = createdDevice.Id;
            }
            else
            {
              deviceEntity.Id = _editingEntity.Id;
              RelaySwitchModules.UpdateAsync(relaySwitchModule).GetAwaiter().GetResult();
            }

            RequestSave?.Invoke(s, deviceEntity);
            RequestCloseWindow();
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

