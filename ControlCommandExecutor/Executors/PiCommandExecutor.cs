using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using ControlCommandExecutor.Execution;
using NewCore.Base.Interface.Main;
using Utilities;
using Utilities.Interface;
using Utilities.Models;

namespace ControlCommandExecutor.Executors
{
  internal class PiCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "ПИ";

    public async Task ExecuteAsync(CommandExecutionContext context)
    {

      if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }

      var command = context.Command as PiCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);


      var time = ExtractFirstNumber(command.Time);
      var voltage = ExtractFirstNumber(command.Voltage);
      string message = string.Empty;

      foreach (var str in command.SourceLines)
      {
        message += "\r\n  " + str;
      }

      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение команды {nameCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: message) { IndentLevel = 1 }, IsBlockStart: true);



      var points = command.Scheme?.GroupModels?
                 .SelectMany(chain => chain?.ChainModels ?? Enumerable.Empty<ChainModel>())
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

      // Первый тест СИ
      if (command.SiCommand != null)
      {
        command.SiCommand.FormattedStartLineNumber = command.FormattedStartLineNumber;
        var commandExecutionContext = new CommandExecutionContext(context.CommandExecutionManager, command.SiCommand, context.Console, context.TranslationControl);
        var siCommandExecutor = new SiCommandExecutor();
        await siCommandExecutor.ExecuteAsync(commandExecutionContext);
      }

      await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение команды {nameCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor) { IndentLevel = 0 });
      var breakDown = await EquipmentService.GetBreakdownTesterOrThrow(context.Console);
      await SettingBreakdown(breakDown, context.Console, time.Value, voltage.Value, command.VoltageType);

      if (command.AlgorithmKey.Contains("К"))
      {
        BaseStrategies.NodeFullChecker.PerformMeasurementAsync measure = NodeFullPerformMeasurementAsync;
        await BaseStrategies.NodeFullChecker.CheckSequenceAsync(command.Scheme, measure, context.CommandExecutionManager, command, context.Console, 80);
      }
      else if (command.AlgorithmKey.Contains("Г"))
      {
        BaseStrategies.NodeFullChecker.PerformMeasurementAsync measure = NodeFullPerformMeasurementAsync;
        await BaseStrategies.MethodExecutor.CheckSequenceAsync(command.Scheme, measure, context.CommandExecutionManager, command, context.Console, 80);
      }
      else if (command.AlgorithmKey.Contains("Т1"))
      {
        BaseStrategies.NodeAccumulationChecker.PerformMeasurementAsync measure = NodeAccumulationPerformMeasurementAsync;
        await BaseStrategies.PairwiseFirstPointChecker.CheckSequenceAsync(command.Scheme, measure, context.CommandExecutionManager, command, context.Console, 80);
      }
      else
      {
        BaseStrategies.NodeAccumulationChecker.PerformMeasurementAsync measure = NodeAccumulationPerformMeasurementAsync;
        await BaseStrategies.NodeAccumulationChecker.CheckSequenceAsync(command.Scheme, context.CommandExecutionManager, command, measure, context.Console, context.Console.GetCancellationToken(), 80);
      }

      //Второй тест СИ
      if (command.SiCommand != null)
      {
        var commandExecutionContext = new CommandExecutionContext(context.CommandExecutionManager, command.SiCommand, context.Console, context.TranslationControl);
        var siCommandExecutor = new SiCommandExecutor();
        await siCommandExecutor.ExecuteAsync(commandExecutionContext);
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

    private async Task SettingsDeviceBusCommutatuion(ISwitchingDevice dbc, IUserMessageService userMessageService)
    {
      await dbc.ConnectableManager.ResetAsync(userMessageService);
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => dbc.ConnectorManager.ConnectBreakdownTester(), userMessageService))
      {
        throw AppConfiguration.Error.Device.DeviceBusCommutation.ConnectorExceptionFactory.ConnectBreakdownFailed(dbc.Name, dbc.NumberChassis, dbc.Number);
      }
    }

    private async Task SettingBreakdown(IBreakdownTester breakDown, IUserMessageService userMessageService, double time, double voltage, VoltageEnum.Type voltageType)
    {
      string name = breakDown.Name;
      int numberChassis = breakDown.NumberChassis;
      int number = breakDown.Number;

      await userMessageService.ShowMessageAsync(new ShowMessageModel("Настройка пробойной установки"));

      if (voltageType == VoltageEnum.Type.ACW)
      {
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.ConnectableManager.InitializeAsync()).Connect, userMessageService))
        {
          throw AppConfiguration.Error.Device.ConnectionExceptionFactory.ConnectFailed(name, numberChassis, number);
        }

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.AcwManger.SetModeAsync()).Success, userMessageService))
        {
          throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetModeFailed(name, numberChassis, number);
        }

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.AcwManger.SetTestTimeAsync(time)).Success, userMessageService))
        {
          throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetTestTimeFailed(name, numberChassis, number);
        }

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.AcwManger.SetVoltageAsync(voltage)).Success, userMessageService))
        {
          throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
        }

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.AcwManger.SetHighCurrentLimitAsync(80)).Success, userMessageService))
        {
          throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
        }

        if (time == 60)
        {
          if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.AcwManger.SetRampTimeAsync(voltage / 100)).Success, userMessageService))
          {
            throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
          }
        }
        else
        {
          if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.AcwManger.SetRampTimeAsync(0.1)).Success, userMessageService))
          {
            throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
          }
        }
      }
      else if (voltageType == VoltageEnum.Type.DCW)
      {
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.ConnectableManager.InitializeAsync()).Connect, userMessageService))
        {
          throw AppConfiguration.Error.Device.ConnectionExceptionFactory.ConnectFailed(name, numberChassis, number);
        }

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetModeAsync()).Success, userMessageService))
        {
          throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetModeFailed(name, numberChassis, number);
        }

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetTestTimeAsync(time)).Success, userMessageService))
        {
          throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetTestTimeFailed(name, numberChassis, number);
        }

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetVoltageAsync(voltage)).Success, userMessageService))
        {
          throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
        }

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetHighCurrentLimitAsync(80)).Success, userMessageService))
        {
          throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
        }

        if (time == 60)
        {
          if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetRampTimeAsync(voltage / 100)).Success, userMessageService))
          {
            throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
          }
        }
        else
        {
          if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetRampTimeAsync(0.1)).Success, userMessageService))
          {
            throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
          }
        }
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
    private static async Task<bool> NodeAccumulationPerformMeasurementAsync(double value, IUserMessageService messageService, CancellationToken cancellationToken, VoltageEnum.Type type = VoltageEnum.Type.ACW)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        if (type == VoltageEnum.Type.ACW)
        {
          var answer = await breadDown.AcwManger.MeasureCurrentAsync(value);
          var result = !await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled() ? answer < value : !await AppConfiguration.Execution.ExecutionConfig.GetIsErrorSimulationEnabled();
          await messageService.ShowMessageAsync(new ShowMessageModel("Результат измерения прочности изоляции", message: $"{answer} мА", type: (result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 1 }, skipPause: true);
          return result;
        }
        else
        {
          var answer = await breadDown.DcwManger.MeasureCurrentAsync(value);
          var result = !await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled() ? answer < value : !await AppConfiguration.Execution.ExecutionConfig.GetIsErrorSimulationEnabled();
          await messageService.ShowMessageAsync(new ShowMessageModel("Результат измерения прочности изоляции", message: $"{answer} мА", type: (result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 1 }, skipPause: true);
          return result;
        }

      }, messageService);

      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private static async Task<(bool, double)> NodeFullPerformMeasurementAsync(double value, IUserMessageService messageService, CancellationToken cancellationToken, VoltageEnum.Type typeVoltage = VoltageEnum.Type.ACW)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);
      double answer = -1;
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();

        await messageService.ShowMessageAsync(new ShowMessageModel("Измерение прочности изоляции"));

        if (typeVoltage == VoltageEnum.Type.ACW)
        {
          answer = await breadDown.AcwManger.MeasureCurrentAsync(value);
          var type = ShowMessageModel.MessageType.Success;
          if (answer >= value)
          {
            type = ShowMessageModel.MessageType.Error;
          }

          return type == ShowMessageModel.MessageType.Success ? true : false;
        }
        else
        {
          answer = await breadDown.DcwManger.MeasureCurrentAsync(value);
          var type = ShowMessageModel.MessageType.Success;
          if (answer >= value)
          {
            type = ShowMessageModel.MessageType.Error;
          }

          return type == ShowMessageModel.MessageType.Success ? true : false;
        }
      }, messageService);

      return (result, answer);
    }
  }
}
