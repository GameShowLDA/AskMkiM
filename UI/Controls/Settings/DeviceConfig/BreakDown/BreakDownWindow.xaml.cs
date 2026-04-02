using Ask.Core.Services.App;
using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using DataBaseConfiguration.Services.Device;
using System.Windows;
using UI.Controls.Settings.DeviceConfig.Base;
using UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;

namespace UI.Controls.Settings.DeviceConfig.BreakDown
{
  /// <summary>
  /// Логика взаимодействия для BreakDownWindow.xaml.
  /// </summary>
  public partial class BreakDownWindow : Window, IDataProcessor
  {
    public Action? CloseActionOverride { get; set; }
    private BreakdownTesterEntity? _editingEntity;
    /// <summary>
    /// Событие запроса закрытия окна.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие запроса сохранения данных устройства.
    /// </summary>
    public event EventHandler<BreakdownTesterEntity> RequestSave;

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
    /// Инициализирует новый экземпляр класса <see cref="BreakDownWindow"/>.
    /// </summary>
    public BreakDownWindow()
    {
      InitializeComponent();
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
    /// Устанавливает настройки для пробойной установки.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Экземпляр головного устройства.</param>
    public void SetSettings(object? sender, IHeadUnit e, BreakdownTesterEntity? editingEntity = null)
    {
      _editingEntity = editingEntity;
      deviceSettingsWindow.NameDevice = "Пробойная установка";
      deviceSettingsWindow.LoadDeviceModels<IBreakdownTester>();
      deviceSettingsWindow.SetHeadUnit(e);
      if (editingEntity != null)
      {
        deviceSettingsWindow.LoadFromDevice(editingEntity);
      }

      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        BreakdownTesterEntity deviceEntity = processor.ProcessDevice<BreakdownTesterEntity>(
            selectedDevice: baseDevice as IDevice,
            control: deviceSettingsWindow,
            additionalDataProcessor: this);

        if (deviceEntity != null)
        {
          deviceEntity.PiMaxVoltage = (baseDevice as IBreakdownTester).PiMaxVoltage;
          deviceEntity.SiMaxVoltage = (baseDevice as IBreakdownTester).SiMaxVoltage;
          var svc = ServiceLocator.GetRequired<BreakdownTesterServices>();

          try
          {
            if (_editingEntity == null)
            {
              svc.Create(deviceEntity);
            }
            else
            {
              deviceEntity.Id = _editingEntity.Id;
              svc.Update(deviceEntity);
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

