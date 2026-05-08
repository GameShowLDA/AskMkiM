using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.DataBase.Engine.Static.Devices;
using System.Threading.Tasks;
using System.Windows;
using UI.Controls.Settings.DeviceConfig.Base;
using UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;
using static UI.Controls.Settings.DeviceConfig.DeviceConfigNotifications;

namespace UI.Controls.Settings.DeviceConfig.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Логика взаимодействия для ModuleVoltageCurrentSourceWindow.xaml.
  /// </summary>
  public partial class ModuleVoltageCurrentSourceWindow : Window, IDataProcessor
  {
    public Action? CloseActionOverride { get; set; }
    private PowerSourceModuleDto? _editingDto;

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
    public void SetSettings(object? sender, IHeadUnit e, PowerSourceModuleDto? editingEntity = null)
    {
      _editingDto = editingEntity;
      deviceSettingsWindow.NameDevice = "МИНТ";
      deviceSettingsWindow.LoadDeviceModels<IPowerSourceModule>();
      deviceSettingsWindow.SetHeadUnit(e);
      if (editingEntity != null)
      {
        deviceSettingsWindow.LoadFromDevice(editingEntity);
      }

      deviceSettingsWindow.SaveEvent += async (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        PowerSourceModuleDto deviceDto = processor.ProcessDevice<PowerSourceModuleDto>(
            selectedDevice: baseDevice as IDevice,
            control: deviceSettingsWindow,
            additionalDataProcessor: this);

        if (deviceDto != null)
        {
          try
          {
            if (_editingDto != null)
            {
              deviceDto.Id = _editingDto.Id;
            }

            var powerSourceModule = PowerSourceModules.Build(deviceDto);

            if (_editingDto == null)
            {
              var createdDevice = await PowerSourceModules.CreateAsync(powerSourceModule);
              deviceDto.Id = createdDevice.Id;
              ShowCreated(deviceDto);
            }
            else
            {
              await PowerSourceModules.UpdateAsync(powerSourceModule);
              ShowUpdated(deviceDto);
            }

            RequestCloseWindow();
            RequestSave?.Invoke(s, deviceDto);
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
