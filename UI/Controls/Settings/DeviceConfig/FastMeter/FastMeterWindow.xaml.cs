using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.DataBase.Engine.Static.Devices;
using DataBaseConfiguration.Services.Device;
using System.Windows;
using UI.Controls.Settings.DeviceConfig.Base;
using UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;

namespace UI.Controls.Settings.DeviceConfig.FastMeter
{
  /// <summary>
  /// Логика взаимодействия для FastMeterWindow.xaml.
  /// </summary>
  public partial class FastMeterWindow : Window, IDataProcessor
  {
    public Action? CloseActionOverride { get; set; }
    private FastMeterEntity? _editingEntity;

    /// <summary>
    /// Событие, вызываемое при закрытии окна.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие, вызываемое при сохранении данных измерителя.
    /// </summary>
    public event EventHandler<FastMeterEntity> RequestSave;

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
    public void SetSettings(object? sender, IHeadUnit e, FastMeterEntity? editingEntity = null)
    {
      _editingEntity = editingEntity;
      deviceSettingsWindow.NameDevice = "Измеритель (быстрый)";
      deviceSettingsWindow.LoadDeviceModels<IFastMeter>();
      deviceSettingsWindow.SetHeadUnit(e);
      if (editingEntity != null)
      {
        deviceSettingsWindow.LoadFromDevice(editingEntity);
      }

      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        FastMeterEntity deviceEntity = processor.ProcessDevice<FastMeterEntity>(
            selectedDevice: baseDevice as IDevice,
            control: deviceSettingsWindow,
            additionalDataProcessor: this);

        if (deviceEntity != null)
        {
          deviceEntity.MaxContinuityResistance = (baseDevice as IFastMeter).MaxContinuityResistance;

          try
          {
            if (_editingEntity == null)
            {
              FastMeters.CreateAsync(deviceEntity).GetAwaiter().GetResult();
            }
            else
            {
              deviceEntity.Id = _editingEntity.Id;
              FastMeters.UpdateAsync(deviceEntity).GetAwaiter().GetResult();
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

