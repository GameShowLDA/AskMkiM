using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.DataBase.Engine.Static.Devices;
using System.Threading.Tasks;
using System.Windows;
using UI.Controls.Settings.DeviceConfig.Base;
using UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;
using static UI.Controls.Settings.DeviceConfig.DeviceConfigNotifications;

namespace UI.Controls.Settings.DeviceConfig.FastMeter
{
  /// <summary>
  /// Логика взаимодействия для FastMeterWindow.xaml.
  /// </summary>
  public partial class FastMeterWindow : Window, IDataProcessor
  {
    public Action? CloseActionOverride { get; set; }
    private FastMeterDto? _editingDto;

    /// <summary>
    /// Событие, вызываемое при закрытии окна.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие, вызываемое при сохранении данных измерителя.
    /// </summary>
    public event EventHandler<FastMeterDto> RequestSave;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FastMeterWindow"/>.
    /// </summary>
    public FastMeterWindow()
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
    /// Устанавливает настройки для быстрого измерителя.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Экземпляр головного устройства.</param>
    public void SetSettings(object? sender, IHeadUnit e, FastMeterDto? editingEntity = null)
    {
      _editingDto = editingEntity;
      deviceSettingsWindow.NameDevice = "Измеритель (быстрый)";
      deviceSettingsWindow.LoadDeviceModels<IFastMeter>();
      deviceSettingsWindow.SetHeadUnit(e);
      if (editingEntity != null)
      {
        deviceSettingsWindow.LoadFromDevice(editingEntity);
      }

      deviceSettingsWindow.SaveEvent += async (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        FastMeterDto deviceDto = processor.ProcessDevice<FastMeterDto>(
            selectedDevice: baseDevice as IDevice,
            control: deviceSettingsWindow,
            additionalDataProcessor: this);

        if (deviceDto != null)
        {
          deviceDto.MaxContinuityResistance = (baseDevice as IFastMeter).MaxContinuityResistance;

          try
          {
            if (_editingDto != null)
            {
              deviceDto.Id = _editingDto.Id;
            }

            var fastMeter = FastMeters.Build(deviceDto);

            if (_editingDto == null)
            {
              var createdDevice = await FastMeters.CreateAsync(fastMeter);
              deviceDto.Id = createdDevice.Id;
              ShowCreated(deviceDto);
            }
            else
            {
              await FastMeters.UpdateAsync(fastMeter);
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
