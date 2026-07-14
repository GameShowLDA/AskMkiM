using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

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
      var managerShassi = (await SelfCheckDeviceRuntime.GetChassisManagersAsync(cancellationToken)).FirstOrDefault();
      if (managerShassi == null)
      {
        return;
      }

      var meter = (await SelfCheckDeviceRuntime.GetFastMetersByNumberChassisAsync(managerShassi.Number, cancellationToken)).FirstOrDefault();
      if (meter == null)
      {
        return;
      }

      var dbc = (await SelfCheckDeviceRuntime.GetSwitchingDevicesByNumberChassisAsync(managerShassi.Number, cancellationToken)).FirstOrDefault();
      var mkr = await SelfCheckDeviceRuntime.GetRelaySwitchModulesByNumberChassisAsync(managerShassi.Number, cancellationToken);

      await dbc.SelfTestManager.StartSelfCheck(_messageService.GetCancellationToken(), SwitchingDeviceTypeConnector.FullCheck, _messageService, dbc, meter);

      foreach (var item in mkr)
      {
        await item.SelfTestManager.StartSelfCheck(_messageService.GetCancellationToken(), RelaySwitchTypeConnector.FullCheck, _messageService, dbc);
      }
    }
  }
}
