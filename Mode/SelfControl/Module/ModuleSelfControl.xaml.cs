using System.Windows;
using System.Windows.Controls;
using Core.ConfigCollector;
using Core.Enum;
using Core.Model;

namespace Mode.SelfControl.Module
{
  /// <summary>
  /// Логика взаимодействия для ModuleSelfControl.xaml
  /// </summary>
  public partial class ModuleSelfControl : UserControl
  {
    ChoiсeDevice choiceDevice = new ChoiсeDevice();
    public ModuleSelfControl()
    {
      InitializeComponent();
      InitializeUserControl();
    }

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

    private void EventHandler()
    {
      ProtocolSelfCheckControl.StartMeasureResistanceButtonPreviewMouseDown += (s, a) => Start();
      ProtocolSelfCheckControl.ExitButtonPreviewMouseDown += (s, a) => Exit();
    }

    private void PreAction()
    {
      choiceDevice.Visibility = Visibility.Collapsed;
    }

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

    private void Exit()
    {
      choiceDevice.Visibility = Visibility.Visible;
    }
  }
}
