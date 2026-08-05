using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class NeCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetCommandDisplayInfo(MeasurementTypeCommand.NE).DisplayName;
    private double firstValue = 0;
    private double secondValue = -1;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      firstValue = 0;
      secondValue = 10000000;

      var command = GetRequiredCommand<NeCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);
      SetActiveLine(context, command);

      await CommandMessages.ShowCommandExecutionAsync(context.Console, nameCommand, message);

      List<ShowMessageModel> errorMessage = new();
      List<ShowMessageModel> infoMessage = new();

      await DeviceManager.ShowDevicesPreparationMessageIfNeededAsync(context);

      var points = DeviceManager.RelayModule.PointManager.CollectPoints(command);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      var relayModules = DeviceManager.RelayModule.PrepareRelayModules(points, context);
      await DeviceManager.RelayModule.BusManager.ConnectAllBusLinesAsync(relayModules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();
      await DeviceManager.SwitchModuleManager.DeviceConnectionManager.ConnectMultimeter(dbc, context.Console);

      var meter = await EquipmentService.GetFastMeterOrThrow(context.Console);
      await SettingMeter(meter, context.Console);

      if (command.LowerLimitVoltage.HasValue)
      {
        firstValue = command.LowerLimitVoltage.Value;
      }

      if (command.HigherLimitVoltage.HasValue)
      {
        secondValue = command.HigherLimitVoltage.Value;
      }

      ConnectedPointContext pointContext = new ConnectedPointContext();
      ConnectedPointChecker.PerformMeasurementAsync measure =
        (value, messageService, cancellationToken, errorResistance) =>
          DioideMeasure(value, messageService, cancellationToken, pointContext, errorResistance);

      pointContext.SchemeModel = command.Scheme;
      pointContext.CommandManager = context.CommandExecutionManager;
      pointContext.CommandModel = command;
      pointContext.MessageService = context.Console;
      pointContext.LowerLimit = firstValue;
      pointContext.HigherLimit = secondValue;
      pointContext.PerformMeasurementAsync = measure;
      pointContext.Unit = "В";
      pointContext.UnitMnemonic = "Г";
      pointContext.TypeCommand = MeasurementTypeCommand.NE;

      if (secondValue != -1)
      {
        pointContext.Value = (firstValue + secondValue) / 2;
      }
      else
      {
        pointContext.Value = firstValue + 10;
      }

      var messageResult = await ConnectedPointChecker.CheckSequenceAsync(pointContext);
      errorMessage.AddRange(messageResult.Errors);
      infoMessage.AddRange(messageResult.Info);

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
    private async Task<(bool, double)> DioideMeasure(
      double value,
      IUserInteractionService messageService,
      CancellationToken cancellationToken,
      ConnectedPointContext pointContext,
      double errorResistance = 0)
    {
      var meter = await EquipmentService.GetFastMeterOrThrow(messageService);
      double answer = 0;

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        answer = await GetDiodeMeasurementValueAsync(meter, value, pointContext, messageService);

        if (answer < 0)
        {
          answer = 0;
        }

        MeasurementRange measurementRange = new MeasurementRange(answer, firstValue, secondValue);
        var measurementResult = MeasurementResultEvaluator.Evaluate(
          measurementRange,
          pointContext.IsOverloadExpected);
        await MeasurementMessages.PublishResultAsync(
          MeasurementTypeCommand.NE,
          new MeasurementRange(
            measurementResult.Value,
            measurementRange.LowerBound,
            measurementRange.UpperBound),
          measurementResult.IsSuccessful,
          outputService: messageService);
        return measurementResult;
      }, messageService);

      return result;
    }

    /// <summary>
    /// Возвращает значение проверки диода с учётом холостого режима и ожидаемой перегрузки.
    /// </summary>
    private async Task<double> GetDiodeMeasurementValueAsync(
      IMultimeter meter,
      double value,
      ConnectedPointContext pointContext,
      IUserInteractionService messageService)
    {
      if (await ShouldReturnOverloadInIdleReverseModeAsync(pointContext))
      {
        return 9.9E+37;
      }

      MeasurementRange measurementRange = new MeasurementRange(value, firstValue, secondValue);
      return await meter.DiodeManager.CheckDiodeAsync(measurementRange, messageService);
    }

    /// <summary>
    /// Определяет, нужно ли в холостом режиме вернуть перегрузку для обратного направления NE.
    /// </summary>
    /// <param name="pointContext">Контекст проверки соединённых точек.</param>
    /// <returns>
    /// Задача, результат которой равен <see langword="true"/>, если требуется вернуть признак перегрузки.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    private static async Task<bool> ShouldReturnOverloadInIdleReverseModeAsync(ConnectedPointContext pointContext) =>
      ExecutionConfig.GetIsIdleModeEnabled()
      && !ExecutionConfig.GetIsErrorSimulationEnabled()
      && pointContext.IsOverloadExpected;

    private async Task SettingMeter(IMultimeter meter, IUserInteractionService userMessageService)
    {
      await meter.DiodeManager.SetDiodeModeAsync(userMessageService);
    }
  }
}
