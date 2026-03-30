using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.DataBase.Engine.Static.Devices;
using System.Windows;
using UI.Controls.Settings.DeviceConfig.Base;
using UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;

namespace UI.Controls.Settings.DeviceConfig.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Логика взаимодействия для ModuleVoltageCurrentSourceWindow.xaml.
  /// </summary>
  public partial class ModuleVoltageCurrentSourceWindow : Window, IDataProcessor
  {
    public Action? CloseActionOverride { get; set; }
    private PowerSourceModuleEntity? _editingEntity;

    /// <summary>
    /// Событие, вызываемое при закрытии окна.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие, вызываемое при сохранении данных модуля источника питания.
    /// </summary>
    public event EventHandler<PowerSourceModuleDto> RequestSave;

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
    /// Устанавливает настройки для модуля источника питания.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Экземпляр головного устройства.</param>
    public void SetSettings(object? sender, IHeadUnit e, PowerSourceModuleEntity? editingEntity = null)
    {
      _editingEntity = editingEntity;
      deviceSettingsWindow.NameDevice = "МИНТ";
      deviceSettingsWindow.LoadDeviceModels<IPowerSourceModule>();
      deviceSettingsWindow.SetHeadUnit(e);
      if (editingEntity != null)
      {
        deviceSettingsWindow.LoadFromDevice(editingEntity);
      }

      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        PowerSourceModuleDto deviceDto = processor.ProcessDevice<PowerSourceModuleDto>(
            selectedDevice: baseDevice as IDevice,
            control: deviceSettingsWindow,
            additionalDataProcessor: this);

        if (deviceDto != null)
        {
          var powerSourceModule = PowerSourceModules.Build(deviceDto);
          try
          {
            if (_editingEntity == null)
            {
              PowerSourceModules.CreateAsync(powerSourceModule).GetAwaiter().GetResult();
            }
            else
            {
              deviceDto.Id = _editingEntity.Id;
              PowerSourceModules.UpdateAsync(powerSourceModule).GetAwaiter().GetResult();
            }

            RequestSave?.Invoke(s, deviceDto);
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

