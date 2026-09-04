using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class PiCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetCommandDisplayInfo(MeasurementTypeCommand.PI).DisplayName;
    private double amperhMaxDCW = 10;
    private double amperhMaxACW = 50;
    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<PiCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}/{command.CommandNumber} {command.Mnemonic}";
      var message = CommandMessages.FormatSourceLinesWithHeader(
        $"{command.CommandNumber} {command.Mnemonic}/ПИ1",
        command.SourceLines);

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
        commandExecutionContext.ProtocolSourceLines = command.SourceLines;
        var siCommandExecutor = new SiCommandExecutor();
        await siCommandExecutor.ExecuteAsync(commandExecutionContext, protocolModel);
        command.Scheme.SetErrorChainDisconnectedPoints(command.SiCommand.Scheme.GetErrorChainDisconnectedPoints());
      }

      await CommandMessages.PublishCommandExecutionAsync(context.Console, nameCommand, message);
      var breakDown = await EquipmentService.GetBreakdownTesterOrThrow(context.Console);
      await SettingBreakdown(breakDown, context.Console, time.Value, voltage.Value, command.VoltageType);

      var executionResult = new AlgorithmExecutionResult(new(), new());

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
        executionResult.AddRange(await NodeFullChecker.CheckSequenceAsync(nodeFullContext));
      }
      else if (command.AlgorithmKey.Contains("Г"))
      {
        methodExecutionContext.PerformMeasurementAsync = NodeFullPerformMeasurementAsync;
        executionResult.AddRange(await MethodExecutor.CheckSequenceAsync(methodExecutionContext));
      }
      else if (command.AlgorithmKey.Contains("Т1"))
      {
        pairwiseFirstPointContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;
        executionResult.AddRange(await PairwiseFirstPointChecker.CheckSequenceAsync(pairwiseFirstPointContext));
      }
      else
      {
        nodeAccumulationContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;
        executionResult.AddRange(await NodeAccumulationChecker.CheckSequenceAsync(nodeAccumulationContext));
      }

      await ExecutionMessages.PublishCheckResultsAsync(executionResult.Errors, context.Console);
      protocolModel.AddResult(nameCommand, executionResult);

      await CompleteProtocolCommandAsync(context, protocolModel, nameCommand);

      if (command.SiCommand != null)
      {
        var commandExecutionContext = new CommandExecutionContext(context.CommandExecutionManager, command.SiCommand, context.Console, context.TranslationControl, context.OpkFilePath);
        var siCommandExecutor = new SiCommandExecutor();
        commandExecutionContext.IsInvokedByAnotherCommand = true;
        commandExecutionContext.ProtocolSourceLines = command.SourceLines;

        command.SiCommand.CommandNumber = siCommanNumber + " " + 2;
        await siCommandExecutor.ExecuteAsync(commandExecutionContext, protocolModel);
      }
    }
    private async Task SettingBreakdown(IBreakdownTester breakDown, IUserInteractionService userMessageService, double time, double voltage, VoltageEnum.Type voltageType)
    {
      string name = breakDown.Name;
      int numberChassis = breakDown.NumberChassis;
      int number = breakDown.Number;

      await ExecutionMessages.PublishBreakdownTesterSetupAsync(userMessageService);

      if (voltageType == VoltageEnum.Type.ACW)
      {
        await breakDown.AcwManger.Mode.SetModeAsync(userMessageService);
        await breakDown.AcwManger.Time.SetTestTimeAsync(time, userMessageService);
        await breakDown.AcwManger.Voltage.SetVoltageAsync(voltage, userMessageService);
        await breakDown.AcwManger.CurrentLimits.SetHighCurrentLimitAsync(amperhMaxACW, userMessageService);
        breakDown.Time.SetTargetTime(time);

        if (time == 60)
        {
          await breakDown.AcwManger.Time.SetRampTimeAsync(voltage / 100, userMessageService);
        }
        else
        {
          await breakDown.AcwManger.Time.SetRampTimeAsync(1, userMessageService);
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
          await breakDown.DcwManger.Time.SetRampTimeAsync(0.4, userMessageService);
        }
      }
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> NodeAccumulationPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0, VoltageEnum.Type type = VoltageEnum.Type.DCW, string? points = null)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        if (type == VoltageEnum.Type.ACW)
        {
          MeasurementRange measurementRange = new MeasurementRange(value, 0, amperhMaxACW);
          var answer = await breadDown.AcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandAC, measurementRange);
          measurementRange.TargetValue = answer.Value;
          var result = MeasurementResultEvaluator.Evaluate(measurementRange);
          await MeasurementMessages.PublishInsulationStrengthResultAsync(
            CheckType.ControlProgram,
            points ?? "Точки не определены",
            new MeasurementRange(result.Value, measurementRange.LowerBound, measurementRange.UpperBound),
            CurrentUnit.MilliAmpere,
            result.IsSuccessful,
            messageService);
          return result;
        }
        else
        {
          MeasurementRange measurementRange = new MeasurementRange(value, 0, amperhMaxDCW);
          var answer = await breadDown.DcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandDC, measurementRange);
          measurementRange.TargetValue = answer.Value;
          var result = MeasurementResultEvaluator.Evaluate(measurementRange);
          await MeasurementMessages.PublishInsulationStrengthResultAsync(
            CheckType.ControlProgram,
            points ?? "Точки не определены",
            new MeasurementRange(result.Value, measurementRange.LowerBound, measurementRange.UpperBound),
            CurrentUnit.MilliAmpere,
            result.IsSuccessful,
            messageService);
          return result;
        }

      }, messageService, measurementTask: true);

      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> NodeFullPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0, VoltageEnum.Type typeVoltage = VoltageEnum.Type.DCW, string? points = null)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);
      double answer = -1;
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();
        if (typeVoltage == VoltageEnum.Type.ACW)
        {
          MeasurementRange measurementRange = new MeasurementRange(value, 0, amperhMaxACW);
          answer = (await breadDown.AcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandAC, measurementRange)).Value;
          measurementRange.TargetValue = answer;
          var result = MeasurementResultEvaluator.Evaluate(measurementRange);
          await MeasurementMessages.PublishInsulationStrengthResultAsync(
            CheckType.ControlProgram,
            points ?? "Точки не определены",
            new MeasurementRange(result.Value, measurementRange.LowerBound, measurementRange.UpperBound),
            CurrentUnit.MilliAmpere,
            result.IsSuccessful,
            messageService);
          return result;
        }
        else
        {
          MeasurementRange measurementRange = new MeasurementRange(value, 0, amperhMaxDCW);
          answer = (await breadDown.DcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandDC, measurementRange)).Value;
          measurementRange.TargetValue = answer;
          var result = MeasurementResultEvaluator.Evaluate(measurementRange);
          await MeasurementMessages.PublishInsulationStrengthResultAsync(
            CheckType.ControlProgram,
            points ?? "Точки не определены",
            new MeasurementRange(result.Value, measurementRange.LowerBound, measurementRange.UpperBound),
            CurrentUnit.MilliAmpere,
            result.IsSuccessful,
            messageService);
          return result;
        }
      }, messageService, measurementTask: true);

      return result;
    }

  }
}
