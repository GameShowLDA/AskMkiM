using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
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
    public string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PR).DisplayName;
    private double lowValue = 0;
    private double hightValue = -1;
    private bool continuityManager = true;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<PrCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);

      SetActiveLine(context, command);

      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);
      await DeviceManager.ShowDevicesPreparationMessageIfNeededAsync(context);

      var points = DeviceManager.RelayModule.PointManager.CollectPoints(command);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      var relayModules = DeviceManager.RelayModule.PrepareRelayModules(points, context);
      await DeviceManager.RelayModule.BusManager.ConnectAllBusLinesAsync(relayModules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();
      await DeviceManager.SwitchModuleManager.DeviceConnectionManager.ConnectMultimeter(dbc, context.Console);

      var meter = EquipmentService.GetFastMeterOrThrow(context.Console);

      double resistance = 0;

      await SettingMeter(meter, context.Console);

      MethodExecutionContext methodExecutionContext = new MethodExecutionContext(context, command, command);
      methodExecutionContext.Value = resistance;

      PairwiseFirstPointContext pairwiseFirstPointContext = methodExecutionContext.CreateChild<PairwiseFirstPointContext>();

      List<ShowMessageModel> errorMessage = new();
      List<ShowMessageModel> infoMessage = new();

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
        errorMessage.AddRange(messageResult.errorMessage);
        infoMessage.AddRange(messageResult.infoMessage);
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
        errorMessage.AddRange(messageResult.Errors);
        infoMessage.AddRange(messageResult.Info);
      }

      await PointFormater.MessageResult(errorMessage, context.Console);

      if (errorMessage.Count > 0)
      {
        protocolModel.AddErrors(nameCommand, errorMessage);
      }
      if (infoMessage.Count > 0)
      {
        protocolModel.AddInfo(nameCommand, infoMessage);
      }
    }
    private async Task SettingMeter(IFastMeter meter, IUserInteractionService userMessageService)
    {
      string name = meter.Name;
      int numberChassis = meter.NumberChassis;
      int number = meter.Number;

      if (DeviceDisplayConfig.GetExecutionParametersVisibility())
      {
        await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildMultimeterSetupMessage());
      }

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
      var fastMeter = EquipmentService.GetFastMeterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        double answer = 0;

        if (continuityManager)
        {
          answer = await fastMeter.ContinuityManager.CheckContinuityAsync(resistance, lowValue, hightValue);
        }
        else
        {
          answer = await fastMeter.ResistanceManager.MeasureResistanceAsync(resistance, lowValue, hightValue);
        }

        if (answer < 0)
        {
          answer = 0;
        }

        return await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.PR, lowValue, -1, answer);

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
      var fastMeter = EquipmentService.GetFastMeterOrThrow(messageService);
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        double answer = -1;

        if (continuityManager)
        {
          answer = await fastMeter.ContinuityManager.CheckContinuityAsync(resistance, lowValue, hightValue, messageService);
        }
        else
        {
          answer = await fastMeter.ResistanceManager.MeasureResistanceAsync(resistance, lowValue, hightValue);
        }

        if (answer < 0)
        {
          answer = 0;
        }

        return await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.PR, lowValue, hightValue, answer);
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
      var fastMeter = EquipmentService.GetFastMeterOrThrow(messageService);
      double answer = -1;

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        if (continuityManager)
        {
          answer = await fastMeter.ContinuityManager.CheckContinuityAsync(resistance, lowValue, hightValue, messageService);
        }
        else
        {
          answer = await fastMeter.ResistanceManager.MeasureResistanceAsync(resistance, lowValue, hightValue);
        }

        if (!ExecutionConfig.GetIsIdleModeEnabled())
        {
          answer -= errorResistance;
        }

        if (answer < 0)
        {
          answer = 0;
        }

        var result = answer >= lowValue && answer <= hightValue;

        return await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.PR, lowValue, hightValue, answer);
      }, messageService);

      return result;
    }
  }
    #endregion
}

