using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Device.Runtime.Function.Base.Multimeter.SelfCheck;
using static Ask.Device.Runtime.Function.GPT.SelfCheck.SelfTestManager;

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
      ActionSettings settings = new ActionSettings()
      {
        StartDelegate = ExecuteMeasurementProcess,
        CheckType = CheckType.SelfTest,
        AccumulateErrorMessages = true,
      };

      executionController.SetSettings(settings);
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    private async Task ExecuteMeasurementProcess(ActionSettings settings, IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var managerShassi = ChassisManagers.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
      if (managerShassi == null)
      {
        return;
      }

      var meter = FastMeters.GetDevicesByNumberChassisAsync(managerShassi.Number).GetAwaiter().GetResult().FirstOrDefault();
      if (meter == null)
      {
        return;
      }

      var dbc = (await SwitchingDevices.GetDevicesByNumberChassisAsync(managerShassi.Number)).FirstOrDefault();
      var mkr = await RelaySwitchModules.GetDevicesByNumberChassisAsync(managerShassi.Number);
      var breakdownTester = (await BreakdownTesters.GetDevicesByNumberChassisAsync(managerShassi.Number)).FirstOrDefault();

      await dbc.SelfTestManager.StartSelfCheck(
        _messageService.GetCancellationToken(),
        SwitchingDeviceTypeConnector.FullCheck,
        settings,
        _messageService,
        dbc,
        meter);

      foreach (var item in mkr)
      {
        await item.SelfTestManager.StartSelfCheck(
          _messageService.GetCancellationToken(),
          RelaySwitchTypeConnector.FullCheck,
          settings,
          _messageService,
          dbc);
      }

      await meter.SelfTestManager.StartSelfCheck(
      _messageService.GetCancellationToken(),
      MultimeterTypeConnector.FullCheck,
      settings,
      _messageService,
      dbc,
      meter);

      await breakdownTester.SelfTestManager.StartSelfCheck(
        _messageService.GetCancellationToken(),
        TypeConnector.FullCheck,
        settings,
        _messageService,
        breakdownTester,
        dbc,
        meter);
    }
  }
}
