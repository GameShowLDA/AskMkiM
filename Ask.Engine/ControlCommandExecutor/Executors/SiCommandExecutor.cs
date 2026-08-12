using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using System.Diagnostics;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  /// <summary>
  /// Выполняет команду проверки сопротивления изоляции СИ.
  /// </summary>
  internal class SiCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    /// <summary>
    /// Отображаемое имя команды СИ.
    /// </summary>
    public string Mnemonic => EnumExtensions.GetCommandDisplayInfo(MeasurementTypeCommand.SI).DisplayName;

    /// <summary>
    /// Нижняя граница сопротивления для измерений методом накапливающего узла.
    /// </summary>
    private double firstValue = 0;

    /// <summary>
    /// Выполняет команду СИ и сохраняет обнаруженные ошибки в модели протокола.
    /// </summary>
    /// <param name="context">Контекст выполнения команды.</param>
    /// <param name="protocolModel">Модель протокола программы контроля.</param>
    /// <returns>Задача, представляющая выполнение команды.</returns>
    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<SiCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      string? nestedCommandHeader = null;
      SetActiveLine(context, command);

      if (context.IsInvokedByAnotherCommand)
      {
        try
        {
          nameCommand = $"{command.CommandNumber.Split(' ').First()} ПИ/{command.Mnemonic}{command.CommandNumber.Split(' ').Last()}";
        }
        catch
        {
          nameCommand = $"{command.CommandNumber} ПИ/{command.Mnemonic}";
        }

        nestedCommandHeader = nameCommand;
      }

      var message = nestedCommandHeader == null
        ? CommandMessages.FormatSourceLines(context.ProtocolSourceLines)
        : CommandMessages.FormatSourceLinesWithHeader(nestedCommandHeader, context.ProtocolSourceLines);
      var total = Stopwatch.StartNew();
      await CommandMessages.PublishCommandExecutionAsync(context.Console, nameCommand, message);
      await DeviceManager.ShowDevicesPreparationMessageIfNeededAsync(context);

      var points = DeviceManager.RelayModule.PointManager.CollectPoints(command);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      var relayModules = DeviceManager.RelayModule.PrepareRelayModules(points, context);
      await DeviceManager.RelayModule.BusManager.ConnectAllBusLinesAsync(relayModules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();
      await DeviceManager.SwitchModuleManager.DeviceConnectionManager.ConnectBreakdownTester(dbc, context.Console);

      var breakDown = await EquipmentService.GetBreakdownTesterOrThrow(context.Console);
      await SettingBreakdown(breakDown, context.Console, command.Time.Value, command.Resistance.Value, command.Voltage.Value);

      NodeFullContext nodeFullContext = new NodeFullContext(context, command, command, command.Resistance.Value + 1, command.Resistance.Value, -1);
      nodeFullContext.IsInvokedByAnotherCommand = context.IsInvokedByAnotherCommand;

      MethodExecutionContext methodExecutionContext = nodeFullContext.CreateChild<MethodExecutionContext>();
      NodeAccumulationContext nodeAccumulationContext = nodeFullContext.CreateChild<NodeAccumulationContext>();
      PairwiseFirstPointContext pairwiseFirstPointContext = nodeFullContext.CreateChild<PairwiseFirstPointContext>();
      nodeFullContext.PerformMeasurementAsync = NodeFullPerformMeasurementAsync;
      methodExecutionContext.PerformMeasurementAsync = NodeFullPerformMeasurementAsync;
      pairwiseFirstPointContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;
      nodeAccumulationContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;
      firstValue = command.Resistance.Value;

      DisconnectionCheckRequest disconnectionCheckRequest = new DisconnectionCheckRequest()
      {
        AlgorithmKey = command.AlgorithmKey,
        NodeFullContext = nodeFullContext,
        MethodExecutionContext = methodExecutionContext,
        PairwiseFirstPointContext = pairwiseFirstPointContext,
        NodeAccumulationContext = nodeAccumulationContext
      };

      var messageResult = await DisconnectionCheckExecutor.ExecuteAsync(disconnectionCheckRequest);

      await ExecutionMessages.PublishCheckResultsAsync(messageResult.Errors, context.Console);

      protocolModel.AddResult(nameCommand, messageResult);

      await CompleteProtocolCommandAsync(context, protocolModel, nameCommand);
      LogInformation($"[PERF][SI] total: {total.ElapsedMilliseconds} ms", isDeviceLog: true);
    }

    /// <summary>
    /// Настраивает пробойную установку для измерения сопротивления изоляции.
    /// </summary>
    /// <param name="breakDown">Пробойная установка.</param>
    /// <param name="userMessageService">Сервис вывода сообщений в протокол.</param>
    /// <param name="time">Продолжительность испытания.</param>
    /// <param name="resistance">Нижняя граница сопротивления изоляции.</param>
    /// <param name="voltage">Испытательное напряжение.</param>
    /// <returns>Задача, представляющая настройку пробойной установки.</returns>
    private async Task SettingBreakdown(IBreakdownTester breakDown, IUserInteractionService userMessageService, double time, double resistance, double voltage)
    {
      string name = breakDown.Name;
      int numberChassis = breakDown.NumberChassis;
      int number = breakDown.Number;

      await ExecutionMessages.PublishBreakdownTesterSetupAsync(userMessageService);

      await breakDown.IrManger.Mode.SetModeAsync(userMessageService);
      await breakDown.IrManger.Time.SetTestTimeAsync(time, userMessageService);
      await breakDown.IrManger.ResistanceLimits.SetLowResistanceLimitAsync(resistance, userMessageService);
      await breakDown.IrManger.Voltage.SetVoltageAsync(voltage, userMessageService);
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <param name="value">Заданное значение сопротивления.</param>
    /// <param name="messageService">Сервис вывода сообщений в протокол.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="errorResistance">Сопротивление, используемое при моделировании ошибки.</param>
    /// <param name="typeVoltage">Тип испытательного напряжения.</param>
    /// <returns>Результат проверки и измеренное сопротивление.</returns>
    private async Task<(bool, double)> NodeAccumulationPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0, VoltageEnum.Type typeVoltage = VoltageEnum.Type.ACW)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        MeasurementRange measurementRange = new MeasurementRange(value, firstValue, 60000);

        var measurement = Stopwatch.StartNew();
        var answer = await breadDown.IrManger.Measure.MeasureAsync(ElectricalTestFunction.InsulationResistance, measurementRange);
        measurement.Restart();

        measurementRange.TargetValue = answer.value;
        var result = MeasurementResultEvaluator.Evaluate(measurementRange);
        await MeasurementMessages.PublishResultAsync(CheckType.ControlProgram,
          MeasurementTypeCommand.SI,
          new MeasurementRange(result.Value, measurementRange.LowerBound, measurementRange.UpperBound),
          result.IsSuccessful,
          outputService: messageService);

        return result;
      }, messageService);

      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <param name="value">Заданное значение сопротивления.</param>
    /// <param name="messageService">Сервис вывода сообщений в протокол.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="errorResistance">Сопротивление, используемое при моделировании ошибки.</param>
    /// <param name="typeVoltage">Тип испытательного напряжения.</param>
    /// <returns>Результат проверки и измеренное сопротивление.</returns>
    private async Task<(bool, double)> NodeFullPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0, VoltageEnum.Type typeVoltage = VoltageEnum.Type.ACW)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);
      (double Value, string Unit) answer = (-1, string.Empty);
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();
        MeasurementRange measurementRange = new MeasurementRange(value, value, 60000);

        var measurement = Stopwatch.StartNew();
        answer = await breadDown.IrManger.Measure.MeasureAsync(ElectricalTestFunction.InsulationResistance, measurementRange);

        measurement.Restart();
        measurementRange.TargetValue = answer.Value;
        var result = MeasurementResultEvaluator.Evaluate(measurementRange);
        await MeasurementMessages.PublishResultAsync(CheckType.ControlProgram,
          MeasurementTypeCommand.SI,
          new MeasurementRange(result.Value, measurementRange.LowerBound, measurementRange.UpperBound),
          result.IsSuccessful,
          outputService: messageService);
        return result;

      }, messageService);

      return result;
    }
  }
}
