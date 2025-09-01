using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataBaseConfiguration.Services.Device;
using NewCore.Base.Interface.Main;
using UI.Components;
using Utilities.Models;
using static UI.Components.DeviceSelectorPanel;

namespace Mode.SelfControl.DeviceCheck
{
  /// <summary>
  /// Логика взаимодействия для SystemSelfControl.xaml
  /// </summary>
  public partial class SystemSelfControl : UserControl
  {
    public SystemSelfControl()
    {
      InitializeComponent();
      InitializeSettings();
    }

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings()
    {
      ProtocolUI.SetSettings(
        this,
        StartDelegate: ExecuteMeasurementProcess,
        true,
        checkPower: false);
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    private async Task ExecuteMeasurementProcess(CancellationToken cancellationToken)
    {
      var managerShassi = new DataBaseConfiguration.Services.Device.ChassisManagerServices().GetAllEntities().FirstOrDefault();
      if (managerShassi == null)
      {
        return;
      }

      var meter = new DataBaseConfiguration.Services.Device.FastMeterServices().GetDevicesByNumberChassis(managerShassi.Number).FirstOrDefault();
      if (meter == null)
      {
        return;
      }

      var dbc = new DataBaseConfiguration.Services.Device.SwitchingDeviceServices().GetDevicesByNumberChassis(managerShassi.Number).FirstOrDefault();
      var mkr = new DataBaseConfiguration.Services.Device.RelaySwitchModuleServices().GetDevicesByNumberChassis(managerShassi.Number);

      await dbc.SelfTestManager.StartSelfCheck(ProtocolUI.GetCancellationToken(), ProtocolUI, NewCore.Base.Function.DBC.TypeConnector.FullCheck, dbc, meter);

      foreach (var item in mkr)
      {
        await item.SelfTestManager.StartSelfCheck(ProtocolUI.GetCancellationToken(), NewCore.Base.Function.ModuleRelayControl.TypeConnector.FullCheck, ProtocolUI, dbc);
      }
    }
  }
}
