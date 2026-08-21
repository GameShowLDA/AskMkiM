using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.Chassi;
using static Ask.LogLib.LoggerUtility;

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
      ActionSettings actionSettings = new ActionSettings()
      {
        StartDelegate = ExecuteMeasurementProcess,
        CheckPower = false,
      };
      executionController.SetSettings(actionSettings);
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
      if (managerShassi is ManagerASKMKI)
      {
        var legacySelfTestManager = new SelfTestManager();

        LogInformation($"Системный самоконтроль АСК: старт, стойка={managerShassi.Number}, мультиметр={(meter == null ? "не найден" : $"{meter.Name}({meter.NumberChassis}.{meter.Number})")}.", isDeviceLog: true);
        foreach (var module in Enum.GetValues<LegacyAskSelfControlModule>())
        {
          cancellationToken.ThrowIfCancellationRequested();
          var target = new LegacyAskSelfControlTarget(managerShassi.Number, managerShassi.Name ?? "Тестер АСК", module);
          LogInformation($"Системный самоконтроль АСК: запуск модуля {module}.", isDeviceLog: true);
          await legacySelfTestManager.StartSelfCheck(_messageService.GetCancellationToken(), module, _messageService, target, meter);
        }

        LogInformation($"Системный самоконтроль АСК: завершен, стойка={managerShassi.Number}.", isDeviceLog: true);
        return;
      }

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
