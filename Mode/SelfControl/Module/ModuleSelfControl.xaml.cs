using System.Windows;
using System.Windows.Controls;
using Core.ConfigCollector;
using Core.Enum;
using Core.Model;

namespace Mode.SelfControl.Module
{
  /// <summary>
  /// Представляет пользовательский интерфейс для самоконтроля модуля. 
  /// Отвечает за инициализацию списка устройств, выбор активного устройства и запуск соответствующего процесса самоконтроля.
  /// </summary>
  public partial class ModuleSelfControl : UserControl
  {
    ChoiсeDevice choiceDevice = new ChoiсeDevice();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ModuleSelfControl"/>.
    /// </summary>
    public ModuleSelfControl()
    {
      InitializeComponent();
      InitializeUserControl();
    }

    /// <summary>
    /// Инициализирует пользовательский интерфейс, загружая список устройств и настраивая обработку событий.
    /// </summary>
    public void InitializeUserControl()
    {
      List<DeviceModel> deviceModels = ConfigCollector.GetAllDevices();
      choiceDevice.Height = 50;
      choiceDevice.DeviceSelected += Start;

      foreach (DeviceModel deviceModel in deviceModels)
      {
        if (deviceModel.DeviceType != DeviceEnum.Type.ManagerShassy)
        {
          choiceDevice.AddDevice(deviceModel);
        }
      }

      ProtocolSelfCheckControl.AddContent(choiceDevice);
      EventHandler();
    }

    /// <summary>
    /// Подписывается на события интерфейса для старта измерения и выхода из режима самоконтроля.
    /// </summary>
    private void EventHandler()
    {
      ProtocolSelfCheckControl.StartMeasureResistanceButtonPreviewMouseDown += (s, a) => Start();
      ProtocolSelfCheckControl.ExitButtonPreviewMouseDown += (s, a) => Exit();
    }

    /// <summary>
    /// Скрывает выбор устройства перед началом самоконтроля.
    /// </summary>
    private void PreAction()
    {
      choiceDevice.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Выбирает активное устройство и запускает соответствующий процесс самоконтроля в зависимости от типа устройства.
    /// </summary>
    private void Start()
    {
      DeviceModel deviceModel = choiceDevice.GetActiveDevice();
      if (deviceModel == null)
      {
        return;
      }

      switch (deviceModel.DeviceType)
      {
        case DeviceEnum.Type.DeviceBusCommutation:
          DeviceBusCommutation.Handler deviceBusCommutation = new DeviceBusCommutation.Handler(ProtocolSelfCheckControl, deviceModel);
          ProtocolSelfCheckControl.SetSettings(this, deviceBusCommutation.GetStartDelegate(), false, deviceBusCommutation.GetStopDelegate(), null, PreAction);
          ProtocolSelfCheckControl.Header = "Самоконтроль УКШ";
          break;

        case DeviceEnum.Type.ModuleRelayControl:
          ModuleRelayControl.Handler moduleRelayControl = new ModuleRelayControl.Handler(ProtocolSelfCheckControl, deviceModel);
          ProtocolSelfCheckControl.SetSettings(this, moduleRelayControl.GetStartDelegate(), false, moduleRelayControl.GetStopDelegate(), null, PreAction);
          ProtocolSelfCheckControl.Header = "Самоконтроль МКР";
          break;

        case DeviceEnum.Type.ModuleVoltageCurrentSource:
          ModuleVoltageCurrentSource.Handler moduleVoltageCurrentSource = new ModuleVoltageCurrentSource.Handler(ProtocolSelfCheckControl, deviceModel);
          ProtocolSelfCheckControl.SetSettings(this, moduleVoltageCurrentSource.GetStartDelegate(), false, moduleVoltageCurrentSource.GetStopDelegate(), null, PreAction);
          ProtocolSelfCheckControl.Header = "Самоконтроль МИНТ";
          break;

        case DeviceEnum.Type.AccurateMeter:
          break;

        case DeviceEnum.Type.FastMeter:
          break;
      }
    }

    /// <summary>
    /// Отображает выбор устройства при выходе из режима самоконтроля.
    /// </summary>
    private void Exit()
    {
      choiceDevice.Visibility = Visibility.Visible;
    }
  }
}
