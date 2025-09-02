using System.Text.RegularExpressions;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using ControlCommandAnalyser.Model.Ok;
using ControlCommandExecutor.Execution;
using NewCore.Base.Interface.Main;
using Utilities;
using Utilities.Interface;
using Utilities.Models;

namespace ControlCommandExecutor.Executors
{
  internal class SiCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "СИ";

    public async Task ExecuteAsync(CommandExecutionContext context)
    {
      if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }

      var command = context.Command as SiCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";

      var time = ExtractFirstNumber(command.Time);
      var resistance = ExtractFirstNumber(command.Resistance);
      var voltage = ExtractFirstNumber(command.Voltage);
      string message = string.Empty;

      foreach (var str in command.SourceLines)
      {
        message += "\r\n  " + str;
      }

      await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение команды {nameCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: message) { IndentLevel = 1 }, IsBlockStart: true);

      //var points = (List<PointModel>)(command.Points.Select(x => PointModel.ConvertToPointModels(x.Points)).Where(x => x != null));
      var points = command.Scheme?.ChainModels?
                  .SelectMany(chain => chain?.ChainModels ?? Enumerable.Empty<PartChainModel>())
                  .SelectMany(part => part?.PointModels ?? Enumerable.Empty<PointModel>())
                  .ToList()
                  ?? new List<PointModel>();
      //var points = PointModel.ConvertToPointModels(command.Points);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      await context.Console.ShowMessageAsync(new ShowMessageModel($"Подготовка устройств"));

      var modules = points
          .Select(EquipmentService.GetModuleByPoint)
          .Where(m => m != null)
          .DistinctBy(m => (m.NumberChassis, m.Number))
          .ToList();

      await SettingModuleRelayControl(modules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();

      await SettingsDeviceBusCommutatuion(dbc, context.Console);

      var breakDown = EquipmentService.GetBreakdownTesterOrThrow(context.Console);
      await SettingBreakdown(breakDown, context.Console, time.Value, resistance.Value, voltage.Value);

      if (command.AlgorithmKey.Contains("К"))
      {
        // await NodeFullChecker.CheckSequenceAsync(context.CommandExecutionManager, command, points, context.Console, resistance.Value);
        BaseStrategies.NodeFullChecker.PerformMeasurementAsync measure = NodeFullPerformMeasurementAsync;
        await BaseStrategies.NodeFullChecker.CheckSequenceAsync(command.Scheme, measure, context.CommandExecutionManager, command, context.Console, resistance.Value);
      }
      else if (command.AlgorithmKey.Contains("Г"))
      {
        BaseStrategies.NodeFullChecker.PerformMeasurementAsync measure = NodeFullPerformMeasurementAsync;
        await BaseStrategies.MethodExecutor.CheckSequenceAsync(command.Scheme, measure, context.CommandExecutionManager, command, context.Console, resistance.Value);
        // await MethodExecutor.CheckSequenceAsync(context.CommandExecutionManager, command,  points, context.Console, resistance.Value);
      }
      else if (command.AlgorithmKey.Contains("Т1"))
      {
        BaseStrategies.NodeAccumulationChecker.PerformMeasurementAsync measure = NodeAccumulationPerformMeasurementAsync;
        await BaseStrategies.PairwiseFirstPointChecker.CheckSequenceAsync(command.Scheme, measure, context.CommandExecutionManager, command, context.Console, resistance.Value);
        //await PairwiseFirstPointChecker.CheckSequenceAsync(context.CommandExecutionManager, command, points, context.Console, resistance.Value);
      }
      else
      {
        // await NodeAccumulationChecker.CheckSequenceAsync(points, context.Console, resistance.Value);
        BaseStrategies.NodeAccumulationChecker.PerformMeasurementAsync measure = NodeAccumulationPerformMeasurementAsync;
        await BaseStrategies.NodeAccumulationChecker.CheckSequenceAsync(command.Scheme, context.CommandExecutionManager, command, measure, context.Console, resistance.Value, context.Console.GetCancellationToken());
      }

      if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        await context.Console.ShowMessageAsync(new ShowMessageModel("Сброс устройств") { IndentLevel = 1 });

        await dbc.ConnectableManager.ResetAsync();
        foreach (var item in modules)
        {
          await item.ConnectableManager.ResetAsync();
        }
      }
    }

    private async Task SettingsDeviceBusCommutatuion(ISwitchingDevice dbc, IUserMessageService userMessageService)
    {
      await dbc.ConnectableManager.ResetAsync(userMessageService);

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => dbc.ConnectorManager.ConnectBreakdownTester(), userMessageService))
      {
        throw AppConfiguration.Error.Device.DeviceBusCommutation.ConnectorExceptionFactory.ConnectBreakdownFailed(dbc.Name, dbc.NumberChassis, dbc.Number);
      }
    }
    private async Task SettingModuleRelayControl(List<IRelaySwitchModule> relaySwitchModules, IUserMessageService userMessageService)
    {
      foreach (var module in relaySwitchModules)
      {
        await module.ConnectableManager.ResetAsync();
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.ConnectBusAsync(NewCore.Enum.DeviceEnum.SwitchingBus.A1), userMessageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.BusExceptionFactory.ConnectFailed(NewCore.Enum.DeviceEnum.SwitchingBus.A1.ToString(), module.Name, module.NumberChassis, module.Number);
        }
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.ConnectBusAsync(NewCore.Enum.DeviceEnum.SwitchingBus.B1), userMessageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.BusExceptionFactory.ConnectFailed(NewCore.Enum.DeviceEnum.SwitchingBus.B1.ToString(), module.Name, module.NumberChassis, module.Number);
        }
      }
    }

    private async Task SettingBreakdown(IBreakdownTester breakDown, IUserMessageService userMessageService, double time, double resistance, double voltage)
    {
      string name = breakDown.Name;
      int numberChassis = breakDown.NumberChassis;
      int number = breakDown.Number;

      await userMessageService.ShowMessageAsync(new ShowMessageModel("Настройка пробойной установки"));
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.ConnectableManager.ConnectAsync()).Connect, userMessageService))
      {
        throw AppConfiguration.Error.Device.ConnectionExceptionFactory.ConnectFailed(name, numberChassis, number);
      }

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetModeAsync()).Success, userMessageService))
      {
        throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetModeFailed(name, numberChassis, number);
      }

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetTestTimeAsync(time)).Success, userMessageService))
      {
        throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetTestTimeFailed(name, numberChassis, number);
      }

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetLowResistanceLimitAsync(resistance)).Success, userMessageService))
      {
        throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetLowLimitFailed(name, numberChassis, number);
      }

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetVoltageAsync(voltage)).Success, userMessageService))
      {
        throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
      }
    }

    /// <summary>
    /// Извлекает первое число из строки.
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <returns>Первое найденное число или null, если число не найдено.</returns>
    private static double? ExtractFirstNumber(string input)
    {
      var match = Regex.Match(input, @"-?\d+([.,]\d+)?");
      if (match.Success && double.TryParse(match.Value.Replace(',', '.'), out double result))
        return result;

      return null;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private static async Task<bool> NodeAccumulationPerformMeasurementAsync(double value, IUserMessageService messageService, CancellationToken cancellationToken)
    {
      var breadDown = EquipmentService.GetBreakdownTesterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var answer = await breadDown.IrManger.MeasureResistanceAsync(value);
        var result = !await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled() ? answer >= value : !await AppConfiguration.Execution.ExecutionConfig.GetIsErrorSimulationEnabled();

        await messageService.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления изоляции", message: $"{answer} МОм", type: (result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 1 }, skipPause: true);
        return result;
      }, messageService);

      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private static async Task<(bool, double)> NodeFullPerformMeasurementAsync(double value, IUserMessageService messageService, CancellationToken cancellationToken)
    {
      var breadDown = EquipmentService.GetBreakdownTesterOrThrow(messageService);
      double answer = -1;
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();

        await messageService.ShowMessageAsync(new ShowMessageModel("Измерение сопротивления изоляции"));

        answer = await breadDown.IrManger.MeasureResistanceAsync(value, value, 60000);
        var type = ShowMessageModel.MessageType.Success;
        if (answer < value)
        {
          type = ShowMessageModel.MessageType.Error;
        }

        return type == ShowMessageModel.MessageType.Success ? true : false;
      }, messageService);

      return (result, answer);
    }
  }
}
