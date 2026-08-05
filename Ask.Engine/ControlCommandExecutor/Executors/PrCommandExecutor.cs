using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class PrCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetCommandDisplayInfo(MeasurementTypeCommand.PR).DisplayName;
    private double lowValue = 0;
    private double hightValue = -1;
    private bool continuityManager = true;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<PrCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);

      SetActiveLine(context, command);

      await CommandMessages.ShowCommandExecutionAsync(context.Console, nameCommand, message);
      await DeviceManager.ShowDevicesPreparationMessageIfNeededAsync(context);

      var points = DeviceManager.RelayModule.PointManager.CollectPoints(command);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      var relayModules = DeviceManager.RelayModule.PrepareRelayModules(points, context);
      await DeviceManager.RelayModule.BusManager.ConnectAllBusLinesAsync(relayModules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();
      await DeviceManager.SwitchModuleManager.DeviceConnectionManager.ConnectMultimeter(dbc, context.Console);

      var meter = await EquipmentService.GetFastMeterOrThrow(context.Console);

      double resistance = 0;

      await SettingMeter(meter, context.Console);

      MethodExecutionContext methodExecutionContext = new MethodExecutionContext(context, command, command);
      methodExecutionContext.Value = resistance;

      PairwiseFirstPointContext pairwiseFirstPointContext = methodExecutionContext.CreateChild<PairwiseFirstPointContext>();

      var executionResult = new AlgorithmExecutionResult(new(), new());

      if (!command.AlgorithmKey.Contains("ЗС"))
      {
        lowValue = 0;
        hightValue = -1;

        if (command.ConnectedLowerLimitResistance.HasValue)
        {
          lowValue = command.ConnectedLowerLimitResistance.Value;
        }

        if (command.ConnectedHigherLimitResistance.HasValue)
        {
          hightValue = command.ConnectedHigherLimitResistance.Value;
        }

        if (hightValue >= 1000)
        {
          continuityManager = false;
        }

        methodExecutionContext.LowerLimit = lowValue;
        methodExecutionContext.HigherLimit = hightValue != null ? hightValue : -1;

        ConnectedPointChecker.PerformMeasurementAsync measurePointConnected = ConnectedPointCheckerMeasurementAsync;

        ConnectedPointContext connectedPointContext = methodExecutionContext.CreateChild<ConnectedPointContext>();
        connectedPointContext.PerformMeasurementAsync = measurePointConnected;

        var messageResult = await ConnectedPointChecker.CheckSequenceAsync(connectedPointContext);
        executionResult.AddRange(messageResult);
      }
      if (!command.AlgorithmKey.Contains("ЗР"))
      {
        lowValue = 0;
        hightValue = -1;

        if (command.DisconnectedLowerLimitResistance.HasValue)
        {
          lowValue = command.DisconnectedLowerLimitResistance.Value;
        }

        if (command.DisconnectedHigherLimitResistance.HasValue)
        {
          hightValue = command.DisconnectedHigherLimitResistance.Value;
        }

        if (hightValue >= 1000)
        {
          continuityManager = false;
        }

        methodExecutionContext.LowerLimit = lowValue;
        methodExecutionContext.HigherLimit = hightValue != null ? hightValue : -1;
        methodExecutionContext.Value = lowValue;

        NodeFullContext nodeFullContext = methodExecutionContext.CreateChild<NodeFullContext>();
        nodeFullContext.PerformMeasurementAsync = NodeFullPerformMeasurementAsync;
        methodExecutionContext.PerformMeasurementAsync = NodeFullPerformMeasurementAsync;
        pairwiseFirstPointContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;

        NodeAccumulationContext nodeAccumulationContext = methodExecutionContext.CreateChild<NodeAccumulationContext>();
        nodeAccumulationContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;

        DisconnectionCheckRequest disconnectionCheckRequest = new DisconnectionCheckRequest()
        {
          AlgorithmKey = command.AlgorithmKey,
          NodeFullContext = nodeFullContext,
          MethodExecutionContext = methodExecutionContext,
          PairwiseFirstPointContext = pairwiseFirstPointContext,
          NodeAccumulationContext = nodeAccumulationContext
        };

        var messageResult = await DisconnectionCheckExecutor.ExecuteAsync(disconnectionCheckRequest);
        executionResult.AddRange(messageResult);
      }

      await PointFormater.MessageResult(executionResult.Errors, context.Console);

      if (executionResult.Errors.Count > 0)
      {
        protocolModel.AddErrors(nameCommand, executionResult.Errors);
      }
      if (executionResult.Info.Count > 0)
      {
        protocolModel.AddInfo(nameCommand, executionResult.Info);
      }
    }
    private async Task SettingMeter(IMultimeter meter, IUserInteractionService userMessageService)
    {
      string name = meter.Name;
      int numberChassis = meter.NumberChassis;
      int number = meter.Number;

      await ExecutionMessages.ShowMultimeterSetupAsync(userMessageService);

      if (continuityManager)
      {
        await meter.ContinuityManager.SetContinuityModeAsync(userMessageService);
      }
      else
      {
        await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);
      }
    }

    #region Измерения.

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками метод накапливающего узла.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> NodeAccumulationPerformMeasurementAsync(double resistance, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0, VoltageEnum.Type type = VoltageEnum.Type.ACW)
    {
      var fastMeter = await EquipmentService.GetFastMeterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        double answer = 0;
        MeasurementRange measurementRange = new MeasurementRange(resistance, lowValue, hightValue);

        if (continuityManager)
        {
          answer = await fastMeter.ContinuityManager.CheckContinuityAsync(measurementRange, messageService);
        }
        else
        {
          answer = await fastMeter.ResistanceManager.MeasureResistanceAsync(measurementRange, messageService);
        }

        if (answer < 0)
        {
          answer = 0;
        }

        measurementRange.TargetValue = answer;

        var result = MeasurementResultEvaluator.Evaluate(measurementRange);
        await MeasurementMessages.PublishResultAsync(
          MeasurementTypeCommand.PR,
          new MeasurementRange(result.Value, measurementRange.LowerBound, measurementRange.UpperBound),
          result.IsSuccessful,
          outputService: messageService);
        return result;

      }, messageService);

      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками метод полного узла.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> NodeFullPerformMeasurementAsync(double resistance, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0, VoltageEnum.Type type = VoltageEnum.Type.ACW)
    {
      var fastMeter = await EquipmentService.GetFastMeterOrThrow(messageService);
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        double answer = -1;
        MeasurementRange measurementRange = new MeasurementRange(resistance, lowValue, hightValue);

        if (continuityManager)
        {
          answer = await fastMeter.ContinuityManager.CheckContinuityAsync(measurementRange, messageService);
        }
        else
        {
          answer = await fastMeter.ResistanceManager.MeasureResistanceAsync(measurementRange, messageService);
        }

        if (answer < 0)
        {
          answer = 0;
        }

        measurementRange.TargetValue = answer;
        var result = MeasurementResultEvaluator.Evaluate(measurementRange);
        await MeasurementMessages.PublishResultAsync(
          MeasurementTypeCommand.PR,
          new MeasurementRange(result.Value, measurementRange.LowerBound, measurementRange.UpperBound),
          result.IsSuccessful,
          outputService: messageService);
        return result;
      }, messageService);

      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками методом первой точки.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> ConnectedPointCheckerMeasurementAsync(double resistance, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance)
    {
      var fastMeter = await EquipmentService.GetFastMeterOrThrow(messageService);
      double answer = -1;

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        MeasurementRange measurementRange = new MeasurementRange(resistance, lowValue, hightValue);
        if (continuityManager)
        {
          answer = await fastMeter.ContinuityManager.CheckContinuityAsync(measurementRange, messageService);
        }
        else
        {
          answer = await fastMeter.ResistanceManager.MeasureResistanceAsync(measurementRange, messageService);
        }

        if (!ExecutionConfig.GetIsIdleModeEnabled())
        {
          answer -= errorResistance;
        }

        if (answer < 0)
        {
          answer = 0;
        }

        measurementRange.TargetValue = answer;
        var result = MeasurementResultEvaluator.Evaluate(measurementRange);
        await MeasurementMessages.PublishResultAsync(
          MeasurementTypeCommand.PR,
          new MeasurementRange(result.Value, measurementRange.LowerBound, measurementRange.UpperBound),
          result.IsSuccessful,
          outputService: messageService);
        return result;

      }, messageService);

      return result;
    }
  }
    #endregion
}
