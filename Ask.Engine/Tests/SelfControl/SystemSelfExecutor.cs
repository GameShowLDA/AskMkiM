using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.DataBase.Engine.Static.Devices;

namespace Ask.Engine.Tests.SelfControl
{
  public class SystemSelfExecutor
  {
    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings(IExecutionController executionController)
    {
      executionController.SetSettings(
        StartDelegate: ExecuteMeasurementProcess,
        true,
        checkPower: false);
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
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

      var dbc = (await SwitchingDevices.GetDevicesByNumberChassisAsync(managerShassi.Number)).FirstOrDefault();
      var mkr = new DataBaseConfiguration.Services.Device.RelaySwitchModuleServices().GetDevicesByNumberChassis(managerShassi.Number);

      await dbc.SelfTestManager.StartSelfCheck(_messageService.GetCancellationToken(), SwitchingDeviceTypeConnector.FullCheck, _messageService, dbc, meter);

      foreach (var item in mkr)
      {
        await item.SelfTestManager.StartSelfCheck(_messageService.GetCancellationToken(), RelaySwitchTypeConnector.FullCheck, _messageService, dbc);
      }
    }
  }
}
