using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Device.Runtime.Device.Chassi;
using System.Threading.Tasks;
using System.Windows;
using UI.Controls.Settings.DeviceConfig.Base;
using UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;
using static UI.Controls.Settings.DeviceConfig.DeviceConfigNotifications;

namespace UI.Controls.Settings.DeviceConfig.ChassisManager
{
  /// <summary>
  /// Логика взаимодействия для ChassisManagerWindow.xaml.
  /// </summary>
  public partial class ChassisManagerWindow : Window, IDataProcessor
  {
    public Action? CloseActionOverride { get; set; }

    /// <summary>
    /// Событие запроса закрытия окна.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие запроса сохранения данных устройства.
    /// </summary>
    public event EventHandler<ChassisManagerDto> RequestSave;

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
      return;
    }

    /// <summary>
    /// Устанавливает настройки для теста АСКМ.
    /// </summary>
    public void SetSettings(ChassisManagerDto? existingDevice = null)
    {
      deviceSettingsWindow.NameDevice = "Тест АСКМ";
      if (existingDevice != null)
      {
        deviceSettingsWindow.SetHeadUnit(ChassisManagers.Build(existingDevice));
      }
      deviceSettingsWindow.LoadDeviceModels<IChassisManager>();
      deviceSettingsWindow.LoadFromDevice(existingDevice);

      deviceSettingsWindow.SaveEvent += async (s, a) =>
      {
        try
        {
          var processor = new DeviceSettingsProcessorBase();
          var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

          ChassisManagerDto deviceDto = baseDevice is ManagerASKMKI askChassis
            ? CreateAskChassisDto(askChassis)
            : processor.ProcessDevice<ChassisManagerDto>(
              selectedDevice: baseDevice as IDevice,
              control: deviceSettingsWindow,
              additionalDataProcessor: this);

          if (deviceDto != null)
          {
            deviceDto.BusType = (baseDevice as IChassisManager).BusType;
            var chassi = ChassisManagers.Build(deviceDto);
            if (existingDevice == null)
            {
              var createdDevice = await ChassisManagers.CreateAsync(chassi);
              deviceDto.Id = createdDevice.Id;
              ShowCreated(deviceDto);
            }
            else
            {
              deviceDto.Id = existingDevice.Id;
              await ChassisManagers.UpdateAsync(chassi);
              ShowUpdated(deviceDto);
            }

            RequestCloseWindow();
            RequestSave?.Invoke(s, deviceDto);
          }
        }
        catch (DuplicateEntityException ex)
        {
          var messsage = ex.Message;
          Message.MessageBoxCustom.Show(messsage, "Ошибка сохраненения данных", image: MessageBoxImage.Error);
        }
        catch (ArgumentException ex)
        {
          Message.MessageBoxCustom.Show(ex.Message, "Ошибка сохраненения данных", image: MessageBoxImage.Error);
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

    /// <summary>
    /// Создает DTO стойки АСК с параметрами подключения из формы.
    /// </summary>
    /// <param name="device">Runtime-модель стойки АСК.</param>
    /// <returns>DTO стойки АСК для сохранения в базе данных.</returns>
    private ChassisManagerDto CreateAskChassisDto(ManagerASKMKI device)
    {
      var dto = device.Convert();
      dto.Number = deviceSettingsWindow.NumberDevice;
      dto.ConnectionDetails = BaseHandler<IChassisManager>.GetConnectionDetails(deviceSettingsWindow, device);
      return dto;
    }
  }
}
