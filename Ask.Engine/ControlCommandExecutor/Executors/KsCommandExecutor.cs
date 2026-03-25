using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class KsCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.KC).DisplayName;
    private double firstValue = 0;
    private double secondValue = -1;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      firstValue = 0;
      secondValue = -1;

      var command = GetRequiredCommand<KsCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);
      SetActiveLine(context, command);

      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);

      List<ShowMessageModel> errorMessage = new();
      List<ShowMessageModel> infoMessage = new();

      await DeviceManager.ShowDevicesPreparationMessageIfNeededAsync(context);

      var points = DeviceManager.RelayModule.PointManager.CollectPoints(command);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      var relayModules = DeviceManager.RelayModule.PrepareRelayModules(points, context);
      await DeviceManager.RelayModule.BusManager.ConnectAllBusLinesAsync(relayModules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();
      await DeviceManager.SwitchModuleManager.DeviceConnectionManager.ConnectMultimeter(dbc, context.Console);

      var meter = EquipmentService.GetFastMeterOrThrow(context.Console);
      await SettingFastMeter(meter, context.Console, command.AlgorithmKey.Contains("Б"));

      if (command.LowerLimitResistance.HasValue)
      {
        firstValue = command.LowerLimitResistance.Value;
      }

      if (command.HigherLimitResistance.HasValue)
      {
        secondValue = command.HigherLimitResistance.Value;
      }

      BaseStrategies.ConnectedPointChecker.PerformMeasurementAsync measure;
      if (command.AlgorithmKey.Contains("Б"))
      {
        measure = FastResistanceMeasure;
      }
      else
      {
        measure = ResistanceMeasure;
      }

      ConnectedPointContext pointContext = new ConnectedPointContext();
      pointContext.SchemeModel = command.Scheme;
      pointContext.CommandManager = context.CommandExecutionManager;
      pointContext.CommandModel = command;
      pointContext.MessageService = context.Console;
      pointContext.LowerLimit = firstValue;
      pointContext.HigherLimit = secondValue;
      pointContext.PerformMeasurementAsync = measure;
      pointContext.Unit = "Ом";
      pointContext.UnitMnemonic = "R";
      pointContext.TypeCommand = MeasurementTypeCommand.KC;

      if (command.AlgorithmKey.Contains("Д"))
      {
        pointContext.IsProtocolAttribute = true;
      }

      if (secondValue != -1)
      {
        pointContext.Value = (firstValue + secondValue) / 2;
      }
      else
      {
        pointContext.Value = firstValue + 10;
      }

      var messageResult = await ConnectedPointChecker.CheckSequenceAsync(pointContext);
      errorMessage.AddRange(messageResult.errorMessage);
      infoMessage.AddRange(messageResult.infoMessage);

      if (errorMessage.Count > 0)
      {
        protocolModel.AddErrors(nameCommand, errorMessage);
      }
      if (infoMessage.Count > 0)
      {
        protocolModel.AddInfo(nameCommand, infoMessage);
      }
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> ResistanceMeasure(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0)
    {
      var meter = EquipmentService.GetFastMeterOrThrow(messageService);
      double answer = 0;

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        answer = await meter.ResistanceManager.MeasureResistanceAsync(value, firstValue, secondValue);

        if (!ExecutionConfig.GetIsIdleModeEnabled())
        { 
          answer -= errorResistance;
        }

        if (answer < 0)
        {
          answer = 0;
        }

        return await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.KC, firstValue, secondValue, answer);
      }, messageService);

      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> FastResistanceMeasure(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0)
    {
      var meter = EquipmentService.GetFastMeterOrThrow(messageService);
      double answer = 0;

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        answer = await meter.ContinuityManager.CheckContinuityAsync(value, firstValue, secondValue);

        if (!ExecutionConfig.GetIsIdleModeEnabled())
        {
          answer -= errorResistance;
        }

        if (answer < 0)
        {
          answer = 0;
        }

        return await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.KC, firstValue, secondValue, answer);

      }, messageService);

      return result;
    }

    private async Task SettingFastMeter(IFastMeter meter, IUserInteractionService userMessageService, bool fast = false)
    {
      if (!fast)
      {
        await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);
      }
      else
      {
        await meter.ContinuityManager.SetContinuityModeAsync(userMessageService);
      }
    }
  }
}

