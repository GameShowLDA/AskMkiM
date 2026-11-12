using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Device.Runtime.Function.Base.Multimeter.SelfCheck;
using static Ask.Device.Runtime.Function.GPT.SelfCheck.SelfTestManager;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

      var breakdownTester = (await BreakdownTesters.GetDevicesByNumberChassisAsync(managerShassi.Number)).FirstOrDefault();
      if (breakdownTester != null)
      {
        await breakdownTester.SelfTestManager.StartSelfCheck(
          _messageService.GetCancellationToken(),
          TypeConnector.FullCheck,
          settings,
          _messageService,
          breakdownTester,
          dbc,
          meter);
      }

      var powerSupply = (await PowerSupplies.GetDevicesByNumberChassisAsync(managerShassi.Number)).FirstOrDefault();
      if (powerSupply != null)
      {
        await powerSupply.SelfTestManager.StartSelfCheck(
          _messageService.GetCancellationToken(),
          Ask.Device.Runtime.Function.B5108.SelftTest.SelfTestManager.TypeConnector.Voltage,
          settings,
          _messageService,
          powerSupply,
          meter,
          dbc);
      }
    }
  }
}
