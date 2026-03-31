using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.DataBase.Engine.Static.Devices;
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
    private BreakdownTesterDto? _editingDto;
    /// <summary>
    /// Событие запроса закрытия окна.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие запроса сохранения данных устройства.
    /// </summary>
    public event EventHandler<BreakdownTesterDto> RequestSave;

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
    public void SetSettings(object? sender, IHeadUnit e, BreakdownTesterDto? editingEntity = null)
    {
      _editingDto = editingEntity;
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

        BreakdownTesterDto deviceDto = processor.ProcessDevice<BreakdownTesterDto>(
            selectedDevice: baseDevice as IDevice,
            control: deviceSettingsWindow,
            additionalDataProcessor: this);

        if (deviceDto != null)
        {
          deviceDto.PiMaxVoltage = (baseDevice as IBreakdownTester).PiMaxVoltage;
          deviceDto.SiMaxVoltage = (baseDevice as IBreakdownTester).SiMaxVoltage;

          try
          {
            var breakDown = BreakdownTesters.Build(deviceDto);

            if (_editingDto == null)
            {
              BreakdownTesters.CreateAsync(breakDown).GetAwaiter().GetResult();
            }
            else
            {
              deviceDto.Id = _editingDto.Id;
              BreakdownTesters.UpdateAsync(breakDown).GetAwaiter().GetResult();
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

