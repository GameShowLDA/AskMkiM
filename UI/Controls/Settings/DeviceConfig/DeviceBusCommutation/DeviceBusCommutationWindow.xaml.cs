using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using DataBaseConfiguration.Services.Device;
using System.Windows;
using UI.Controls.Settings.DeviceConfig.Base;
using UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;

namespace UI.Controls.Settings.DeviceConfig.DeviceBusCommutation
{
  /// <summary>
  /// Логика взаимодействия для DeviceBusCommutationWindow.xaml.
  /// </summary>
  public partial class DeviceBusCommutationWindow : Window, IDataProcessor
  {
    public Action? CloseActionOverride { get; set; }
    private SwitchingDeviceEntity? _editingEntity;

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
    /// Устанавливает настройки для устройства коммутации шин.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Экземпляр головного устройства.</param>
    public void SetSettings(object? sender, IHeadUnit e, SwitchingDeviceEntity? editingEntity = null)
    {
      _editingEntity = editingEntity;
      deviceSettingsWindow.NameDevice = "Устройство коммутации шин";
      deviceSettingsWindow.LoadDeviceModels<ISwitchingDevice>();
      deviceSettingsWindow.SetHeadUnit(e);
      if (editingEntity != null)
      {
        deviceSettingsWindow.LoadFromDevice(editingEntity);
      }

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
          try
          {
            if (_editingEntity == null)
            {
              new SwitchingDeviceServices().Create(deviceEntity);
            }
            else
            {
              deviceEntity.Id = _editingEntity.Id;
              new SwitchingDeviceServices().Update(deviceEntity);
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

