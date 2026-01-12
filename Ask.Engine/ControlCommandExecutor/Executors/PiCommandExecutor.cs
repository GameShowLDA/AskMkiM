using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Breakdown;
using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;
using System.Diagnostics.Metrics;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class PiCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PI).DisplayName;
    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {

      var command = context.Command as PiCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);


      var time = command.Time;
      var voltage = command.Voltage;
      string message = string.Empty;

      foreach (var str in command.SourceLines)
      {
        message += "\r\n  " + str;
      }

      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      string nameSiCommand = $"{command.CommandNumber} СИ";

      await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение команды {nameCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: message, type: ShowMessageModel.MessageType.Command) { IndentLevel = 1 }, IsBlockStart: true);



      var points = command.Scheme?.GroupModels?
                 .SelectMany(chain => chain?.ChainModels ?? Enumerable.Empty<ChainModel>())
                 .SelectMany(part => part?.PointModels ?? Enumerable.Empty<PointModel>())
                 .ToList()
                 ?? new List<PointModel>();
      //var points = PointModel.ConvertToPointModels(command.Points);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      if (await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildDevicesPreparationMessage());
      }

      var modules = points
        .Select(EquipmentService.GetModuleByPoint)
        .Where(m => m != null)
        .DistinctBy(m => (m.NumberChassis, m.Number))
        .ToList();

      await SettingModuleRelayControl(modules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();
      await SettingsDeviceBusCommutatuion(dbc, context.Console);
      var siCommanNumber = command.SiCommand.CommandNumber;
      // Первый тест СИ
      if (command.SiCommand != null)
      {
        await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение 1", message: $"{nameSiCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, type: ShowMessageModel.MessageType.CommandBlock) { IndentLevel = 2 }, IsBlockStart: true);
        command.SiCommand.FormattedStartLineNumber = command.FormattedStartLineNumber;
        command.SiCommand.CommandNumber = siCommanNumber + " " + 1;

        var commandExecutionContext = new CommandExecutionContext(context.CommandExecutionManager, command.SiCommand, context.Console, context.TranslationControl, context.OpkFilePath);
        var siCommandExecutor = new SiCommandExecutor();
        await siCommandExecutor.ExecuteAsync(commandExecutionContext, protocolModel);
      }

      await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение 2", message: $"{nameCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, type: ShowMessageModel.MessageType.CommandBlock) { IndentLevel = 2 });
      var breakDown = await EquipmentService.GetBreakdownTesterOrThrow(context.Console);
      await SettingBreakdown(breakDown, context.Console, time.Value, voltage.Value, command.VoltageType);

      List<ShowMessageModel> errorMessage = new();

      NodeAccumulationContext nodeAccumulationContext = new NodeAccumulationContext();
      nodeAccumulationContext.SchemeModel = command.Scheme;
      nodeAccumulationContext.CommandManager = context.CommandExecutionManager;
      nodeAccumulationContext.CommandModel = command;
      nodeAccumulationContext.MessageService = context.Console;
      nodeAccumulationContext.Value = 80;
      nodeAccumulationContext.LowerLimit = 0;
      nodeAccumulationContext.HigherLimit = 80;
      nodeAccumulationContext.Unit = "мА";
      nodeAccumulationContext.UnitMnemonic = "I";
      nodeAccumulationContext.VoltageType = command.VoltageType;

      if (command.VoltageType == VoltageEnum.Type.DCW)
      {
        nodeAccumulationContext.TypeCommand = MeasurementTypeCommand.PI_DCW;
      }
      else
      {
        nodeAccumulationContext.TypeCommand = MeasurementTypeCommand.PI_ACW;
      }

      NodeFullContext nodeFullContext = nodeAccumulationContext.CreateChild<NodeFullContext>();
      MethodExecutionContext methodExecutionContext = nodeAccumulationContext.CreateChild<MethodExecutionContext>();


      //MethodExecutionContext methodExecutionContext = new MethodExecutionContext();
      //nodeFullContext.SchemeModel = command.Scheme;
      //nodeFullContext.CommandManager = context.CommandExecutionManager;
      //nodeFullContext.CommandModel = command;
      //nodeFullContext.MessageService = context.Console;
      //nodeFullContext.Resistance = 80;
      //nodeFullContext.LowerLimit = 0;
      //nodeFullContext.HigherLimit = 80;
      //nodeFullContext.Unit = "мА";
      //nodeFullContext.UnitMnemonic = "I";

      if (command.AlgorithmKey.Contains("К"))
      {
        nodeFullContext.PerformMeasurementAsync = NodeFullPerformMeasurementAsync;
        var errMes = await NodeFullChecker.CheckSequenceAsync(nodeFullContext);
        errorMessage.AddRange(errMes);
      }
      else if (command.AlgorithmKey.Contains("Г"))
      {
        methodExecutionContext.PerformMeasurementAsync = NodeFullPerformMeasurementAsync;
        var errMes = await MethodExecutor.CheckSequenceAsync(methodExecutionContext);
        errorMessage.AddRange(errMes);
      }
      else if (command.AlgorithmKey.Contains("Т1"))
      {
        NodeAccumulationChecker.PerformMeasurementAsync measure = NodeAccumulationPerformMeasurementAsync;
        var errMes = await PairwiseFirstPointChecker.CheckSequenceAsync(command.Scheme, measure, context.CommandExecutionManager, command, context.Console, 80, command.VoltageType);
        errorMessage.AddRange(errMes);
      }
      else
      {
        nodeAccumulationContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;
        var errMes = await NodeAccumulationChecker.CheckSequenceAsync(nodeAccumulationContext);
        errorMessage.AddRange(errMes);
      }

      await PointFormater.MessageResult(errorMessage, context.Console);
      if (errorMessage.Count > 0)
      {
        protocolModel.Errors.Add(nameCommand, errorMessage);
      }

      await context.Console.ShowMessageAsync(new ShowMessageModel("Сброс точек") { IndentLevel = 1 });
      foreach (var item in modules)
      {
        await item.PointManager.DisconnectingAllPoint(context.Console);
      }

      if (command.SiCommand != null)
      {
        await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение 3", message: $"{nameSiCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, type: ShowMessageModel.MessageType.CommandBlock) { IndentLevel = 2 }, IsBlockStart: true);
        var commandExecutionContext = new CommandExecutionContext(context.CommandExecutionManager, command.SiCommand, context.Console, context.TranslationControl, context.OpkFilePath);
        var siCommandExecutor = new SiCommandExecutor();

        command.SiCommand.CommandNumber = siCommanNumber + " " + 2;
        await siCommandExecutor.ExecuteAsync(commandExecutionContext, protocolModel);
      }
    }

    private async Task SettingModuleRelayControl(List<IRelaySwitchModule> relaySwitchModules, IUserInteractionService userMessageService)
    {
      foreach (var module in relaySwitchModules)
      {
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.ConnectBusAsync(SwitchingBus.A1, userMessageService: userMessageService), userMessageService))
        {
          throw BusExceptionFactory.ConnectFailed(SwitchingBus.A1.ToString(), module.Name, module.NumberChassis, module.Number);
        }
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.ConnectBusAsync(SwitchingBus.B1, userMessageService: userMessageService), userMessageService))
        {
          throw BusExceptionFactory.ConnectFailed(SwitchingBus.B1.ToString(), module.Name, module.NumberChassis, module.Number);
        }
      }
    }

    private async Task SettingsDeviceBusCommutatuion(ISwitchingDevice dbc, IUserInteractionService userMessageService)
    {
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => dbc.ConnectorManager.ConnectBreakdownTester(userMessageService), userMessageService))
      {
        throw ConnectorExceptionFactory.ConnectBreakdownFailed(dbc.Name, dbc.NumberChassis, dbc.Number);
      }
    }

    private async Task SettingBreakdown(IBreakdownTester breakDown, IUserInteractionService userMessageService, double time, double voltage, VoltageEnum.Type voltageType)
    {
      string name = breakDown.Name;
      int numberChassis = breakDown.NumberChassis;
      int number = breakDown.Number;

      if (await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildBreakdownTesterSetupMessage());
      }

      if (voltageType == VoltageEnum.Type.ACW)
      {
        await breakDown.AcwManger.Mode.SetModeAsync(userMessageService);
        await breakDown.AcwManger.Time.SetTestTimeAsync(time, userMessageService);
        await breakDown.AcwManger.Voltage.SetVoltageAsync(voltage, userMessageService);
        await breakDown.AcwManger.CurrentLimits.SetHighCurrentLimitAsync(80, userMessageService);

        if (time == 60)
        {
          await breakDown.AcwManger.Time.SetRampTimeAsync(voltage / 100, userMessageService);
        }
        else
        {
          await breakDown.AcwManger.Time.SetRampTimeAsync(0.1, userMessageService);
        }
      }
      else if (voltageType == VoltageEnum.Type.DCW)
      {
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.Mode.SetModeAsync(userMessageService)).Success, userMessageService))
        {
          throw IrExceptionFactory.SetModeFailed(name, numberChassis, number);
        }

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.Time.SetTestTimeAsync(time, userMessageService)).Success, userMessageService))
        {
          throw IrExceptionFactory.SetTestTimeFailed(name, numberChassis, number);
        }

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.Voltage.SetVoltageAsync(voltage, userMessageService)).Success, userMessageService))
        {
          throw IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
        }

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.CurrentLimits.SetHighCurrentLimitAsync(20, userMessageService)).Success, userMessageService))
        {
          throw IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
        }

        if (time == 60)
        {
          if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.Time.SetRampTimeAsync(voltage / 100, userMessageService)).Success, userMessageService))
          {
            throw IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
          }
        }
        else
        {
          if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.Time.SetRampTimeAsync(0.1, userMessageService)).Success, userMessageService))
          {
            throw IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
          }
        }
      }
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private static async Task<(bool, double)> NodeAccumulationPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, VoltageEnum.Type type = VoltageEnum.Type.DCW)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        if (type == VoltageEnum.Type.ACW)
        {
          var answer = (await breadDown.AcwManger.Measure.MeasureAsync(value)).value;
          var result = !await ExecutionConfig.GetIsIdleModeEnabled() ? answer < value : !await ExecutionConfig.GetIsErrorSimulationEnabled();
          if (!result || await DeviceDisplayConfig.GetMeasurementResultsVisibilityAsync())
          {
            await messageService.ShowMessageAsync(new ShowMessageModel("Результат измерения прочности изоляции", message: $"{answer} мА", type: result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
          }
          return (result, answer);
        }
        else
        {
          var answer = (await breadDown.DcwManger.Measure.MeasureAsync(value)).value;
          var result = !await ExecutionConfig.GetIsIdleModeEnabled() ? answer < value : !await ExecutionConfig.GetIsErrorSimulationEnabled();
          await messageService.ShowMessageAsync(new ShowMessageModel("Результат измерения прочности изоляции", message: $"{answer} мА", type: result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
          return (result, answer);
        }

      }, messageService);


      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private static async Task<(bool, double)> NodeFullPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, VoltageEnum.Type typeVoltage = VoltageEnum.Type.DCW)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);
      double answer = -1;
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();

        await messageService.ShowMessageAsync(new ShowMessageModel("Измерение прочности изоляции"));

        if (typeVoltage == VoltageEnum.Type.ACW)
        {
          answer = !await ExecutionConfig.GetIsIdleModeEnabled() ?
                   (await breadDown.AcwManger.Measure.MeasureAsync(10)).value :
                   !await ExecutionConfig.GetIsErrorSimulationEnabled() ? 10 : new Random().Next(80, 150);

          var type = ShowMessageModel.MessageType.Success;
          if (answer >= value)
          {
            type = ShowMessageModel.MessageType.Error;
          }



          return type == ShowMessageModel.MessageType.Success ? true : false;
        }
        else
        {
          answer = (await breadDown.DcwManger.Measure.MeasureAsync(value)).value;
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


    public static async Task<bool> ShowMeasurementResultAsync(IUserInteractionService messageService, double lowerLimit, double upperLimit, double value)
    {
      var result = !await ExecutionConfig.GetIsIdleModeEnabled() ? value >= lowerLimit && value <= upperLimit : !await ExecutionConfig.GetIsErrorSimulationEnabled();

      if (!result || await DeviceDisplayConfig.GetMeasurementResultsVisibilityAsync())
      {
        var message = ExecutorMessageBuilder.BuildMeasurementResultMessage(MeasurementTypeCommand.IE, lowerLimit, upperLimit, value);
        message.Status = value >= lowerLimit && value <= upperLimit ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error;
        message.IndentLevel = 2;

        await messageService.ShowMessageAsync(message, skipPause: true);
      }

      return result;
    }
  }
}
