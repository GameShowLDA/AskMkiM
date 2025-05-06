using System.Windows;
using System.Windows.Controls;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;
using static NewCore.Enum.DeviceEnum;

namespace Mode.SelfControl.NewModule
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
    { }

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
    private async Task PreAction(CancellationToken cancellationToken)
    {
      choiceDevice.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Выбирает активное устройство и запускает соответствующий процесс самоконтроля в зависимости от типа устройства.
    /// </summary>
    private void Start()
    {
      IDevice deviceModel = choiceDevice.GetActiveDevice();
      if (deviceModel == null)
      {
        return;
      }

      switch (deviceModel.DeviceType)
      {
        case DeviceType.SwitchingDevice:
          DeviceBusCommutation.Handler deviceBusCommutation = new DeviceBusCommutation.Handler(ProtocolSelfCheckControl, (ISwitchingDevice)deviceModel);
          ProtocolSelfCheckControl.SetSettings(this, deviceBusCommutation.GetStartDelegate(), false, deviceBusCommutation.GetStopDelegate(), null, PreAction);
          ProtocolSelfCheckControl.Header = "Самоконтроль УКШ";
          break;

        case DeviceType.RelaySwitchModule:
          ModuleRelayControl.Handler moduleRelayControl = new ModuleRelayControl.Handler(ProtocolSelfCheckControl, (IRelaySwitchModule)deviceModel);
          ProtocolSelfCheckControl.SetSettings(this, moduleRelayControl.GetStartDelegate(), false, moduleRelayControl.GetStopDelegate(), null, PreAction);
          ProtocolSelfCheckControl.Header = "Самоконтроль МКР";
          break;

        case DeviceType.PowerSourceModule:
          ModuleVoltageCurrentSource.Handler moduleVoltageCurrentSource = new ModuleVoltageCurrentSource.Handler(ProtocolSelfCheckControl, (IPowerSourceModule)deviceModel);
          ProtocolSelfCheckControl.SetSettings(this, moduleVoltageCurrentSource.GetStartDelegate(), false, moduleVoltageCurrentSource.GetStopDelegate(), null, PreAction);
          ProtocolSelfCheckControl.Header = "Самоконтроль МИНТ";
          break;

        case DeviceType.PrecisionMeter:
          break;

        case DeviceType.FastMeter:
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
