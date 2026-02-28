using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class PiCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PI).DisplayName;
    private double amperhMaxDCW = 10;
    private double amperhMaxACW = 60;
    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<PiCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}/{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);
      message = message.Replace("ПИ", "ПИ/ПИ");

      SetActiveLine(context, command);

      await DeviceManager.ShowDevicesPreparationMessageIfNeededAsync(context);

      var points = DeviceManager.RelayModule.PointManager.CollectPoints(command);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      var relayModules = DeviceManager.RelayModule.PrepareRelayModules(points, context);
      await DeviceManager.RelayModule.BusManager.ConnectAllBusLinesAsync(relayModules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();
      await DeviceManager.SwitchModuleManager.DeviceConnectionManager.ConnectBreakdownTester(dbc, context.Console);

      var time = command.Time;
      var voltage = command.Voltage;
      string nameSiCommand = $"ПИ/СИ1";
      var siCommanNumber = command.SiCommand.CommandNumber;

      if (command.SiCommand != null)
      {
        command.SiCommand.FormattedStartLineNumber = command.FormattedStartLineNumber;
        command.SiCommand.CommandNumber = siCommanNumber + " " + 1;

        var commandExecutionContext = new CommandExecutionContext(context.CommandExecutionManager, command.SiCommand, context.Console, context.TranslationControl, context.OpkFilePath);
        commandExecutionContext.IsInvokedByAnotherCommand = true;
        var siCommandExecutor = new SiCommandExecutor();
        await siCommandExecutor.ExecuteAsync(commandExecutionContext, protocolModel);
        command.Scheme.SetErrorChainDisconnectedPoints(command.SiCommand.Scheme.GetErrorChainDisconnectedPoints());
      }

       await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);
      var breakDown = await EquipmentService.GetBreakdownTesterOrThrow(context.Console);
      await SettingBreakdown(breakDown, context.Console, time.Value, voltage.Value, command.VoltageType);

      List<ShowMessageModel> errorMessage = new();

      NodeAccumulationContext nodeAccumulationContext = new NodeAccumulationContext(context, command, command);
      nodeAccumulationContext.LowerLimit = 0;
      nodeAccumulationContext.VoltageType = command.VoltageType;

      if (command.AlgorithmKey.Contains("И"))
      {
        nodeAccumulationContext.IsPolarityReversed = true;
      }

      if (command.VoltageType == VoltageEnum.Type.DCW)
      {
        nodeAccumulationContext.TypeCommand = MeasurementTypeCommand.PI_DCW;
        nodeAccumulationContext.Value = amperhMaxDCW;
        nodeAccumulationContext.HigherLimit = amperhMaxDCW;
      }
      else
      {
        nodeAccumulationContext.TypeCommand = MeasurementTypeCommand.PI_ACW;
        nodeAccumulationContext.Value = amperhMaxACW;
        nodeAccumulationContext.HigherLimit = amperhMaxACW;
      }

      NodeFullContext nodeFullContext = nodeAccumulationContext.CreateChild<NodeFullContext>();
      MethodExecutionContext methodExecutionContext = nodeAccumulationContext.CreateChild<MethodExecutionContext>();
      PairwiseFirstPointContext pairwiseFirstPointContext = nodeAccumulationContext.CreateChild<PairwiseFirstPointContext>();

      if (command.VoltageType == VoltageEnum.Type.DCW)
      {
        nodeFullContext.VoltageType = VoltageEnum.Type.DCW;
        pairwiseFirstPointContext.VoltageType = VoltageEnum.Type.DCW;
      }
      else
      {
        nodeFullContext.VoltageType = VoltageEnum.Type.ACW;
        pairwiseFirstPointContext.VoltageType = VoltageEnum.Type.ACW;
      }

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
        pairwiseFirstPointContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;
        var errMes = await PairwiseFirstPointChecker.CheckSequenceAsync(pairwiseFirstPointContext);
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
        protocolModel.AddErrors(nameCommand, errorMessage);
      }

      if (command.SiCommand != null)
      {
        var commandExecutionContext = new CommandExecutionContext(context.CommandExecutionManager, command.SiCommand, context.Console, context.TranslationControl, context.OpkFilePath);
        var siCommandExecutor = new SiCommandExecutor();
        commandExecutionContext.IsInvokedByAnotherCommand = true;

        command.SiCommand.CommandNumber = siCommanNumber + " " + 2;
        await siCommandExecutor.ExecuteAsync(commandExecutionContext, protocolModel);
      }
    }
    private async Task SettingBreakdown(IBreakdownTester breakDown, IUserInteractionService userMessageService, double time, double voltage, VoltageEnum.Type voltageType)
    {
      string name = breakDown.Name;
      int numberChassis = breakDown.NumberChassis;
      int number = breakDown.Number;

      if (DeviceDisplayConfig.GetExecutionParametersVisibility())
      {
        await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildBreakdownTesterSetupMessage());
      }

      if (voltageType == VoltageEnum.Type.ACW)
      {
        await breakDown.AcwManger.Mode.SetModeAsync(userMessageService);
        await breakDown.AcwManger.Time.SetTestTimeAsync(time, userMessageService);
        await breakDown.AcwManger.Voltage.SetVoltageAsync(voltage, userMessageService);
        await breakDown.AcwManger.CurrentLimits.SetHighCurrentLimitAsync(amperhMaxACW, userMessageService);

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
        await breakDown.DcwManger.Mode.SetModeAsync(userMessageService);
        await breakDown.DcwManger.Time.SetTestTimeAsync(time, userMessageService);
        await breakDown.DcwManger.Voltage.SetVoltageAsync(voltage, userMessageService);
        await breakDown.DcwManger.CurrentLimits.SetHighCurrentLimitAsync(amperhMaxDCW, userMessageService);

        if (time == 60)
        {
          await breakDown.DcwManger.Time.SetRampTimeAsync(voltage / 100, userMessageService);
        }
        else
        {
          await breakDown.DcwManger.Time.SetRampTimeAsync(0.1, userMessageService);
        }
      }
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> NodeAccumulationPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0, VoltageEnum.Type type = VoltageEnum.Type.DCW)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        if (type == VoltageEnum.Type.ACW)
        {
          var answer = await breadDown.AcwManger.Measure.MeasureAsync(value, 0, amperhMaxACW);
          return await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.PI_ACW, 0, amperhMaxACW, answer.value);
        }
        else
        {
          var answer = await breadDown.DcwManger.Measure.MeasureAsync(value, 0, amperhMaxDCW);
          return await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.PI_DCW, 0, amperhMaxDCW, answer.value);
        }

      }, messageService);


      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> NodeFullPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0, VoltageEnum.Type typeVoltage = VoltageEnum.Type.DCW)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);
      double answer = -1;
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();

        await messageService.ShowMessageAsync(new ShowMessageModel("Измерение прочности изоляции"));

        if (typeVoltage == VoltageEnum.Type.ACW)
        {
          answer = (await breadDown.AcwManger.Measure.MeasureAsync(10)).value;

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


    public async Task<bool> ShowMeasurementResultAsync(IUserInteractionService messageService, double lowerLimit, double upperLimit, double value)
    {
      var result = !ExecutionConfig.GetIsIdleModeEnabled() ? value >= lowerLimit && value <= upperLimit : !await ExecutionConfig.GetIsErrorSimulationEnabled();

      if (!result || DeviceDisplayConfig.GetMeasurementResultsVisibility())
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

