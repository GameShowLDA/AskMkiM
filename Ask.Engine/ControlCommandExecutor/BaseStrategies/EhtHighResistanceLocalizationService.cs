using Ask.Core.Services.UI;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies;

/// <summary>
/// Локализует связные фрагменты цепи ЭТ после превышения верхней границы сопротивления.
/// </summary>
internal static class EhtHighResistanceLocalizationService
{
  /// <summary>
  /// Завершает локализацию исходной цепи, используя результат её первого прохода.
  /// </summary>
  /// <param name="context">Контекст выполнения проверки ЭТ.</param>
  /// <param name="connectedPoints">Точки, связанные с первой точкой исходной цепи.</param>
  /// <param name="unresolvedPoints">Точки, не связанные с первой точкой исходной цепи.</param>
  /// <param name="initialAboveUpperBound">Первое значение выше верхней границы.</param>
  /// <returns>Ошибка с локализованными связными фрагментами.</returns>
  internal static async Task<AlgorithmExecutionResult> LocalizeAsync(
    PairwiseFirstPointAltContext context,
    IReadOnlyList<PointModel> connectedPoints,
    IReadOnlyList<PointModel> unresolvedPoints,
    double initialAboveUpperBound)
  {
    var result = new AlgorithmExecutionResult();
    if (unresolvedPoints.Count == 0)
    {
      return result;
    }

    var localization = await SplitIntoFragmentsAsync(context, unresolvedPoints);
    var fragments = new List<ChainModel>();
    if (connectedPoints.Count > 0)
    {
      fragments.Add(new ChainModel(connectedPoints.ToList()));
    }
    fragments.AddRange(localization.Fragments);

    var display = PointFormater.GetFormatDisconnectedFragments(fragments);
    var range = new MeasurementRange(
      initialAboveUpperBound,
      context.LowerLimit,
      context.HigherLimit);
    var error = MeasurementMessages.BuildMeasurementResultMessage(
      ResistanceUnit.Ohm,
      range,
      false,
      display);

    await MeasurementMessages.PublishBuiltMessageAsync(
      CheckType.ControlProgram,
      error,
      context.MessageService);
    result.Errors.Add(error);
    context.CommandManager.AddErrorMethod(
      EhtErrors.DisconnectedChain(
        $"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}",
        display,
        MeasurementValueFormatter.FormatWithUnit(initialAboveUpperBound, "Ом"),
        context.CommandModel.StartLineNumber,
        context.CommandModel.FormattedStartLineNumber));

    return result;
  }

  /// <summary>
  /// Разбивает неразобранные точки на связные фрагменты с использованием оборудования ЭТ.
  /// </summary>
  /// <param name="context">Контекст выполнения проверки ЭТ.</param>
  /// <param name="points">Точки локализуемой части цепи.</param>
  /// <returns>Связные фрагменты локализуемой части цепи.</returns>
  private static Task<LocalizationResult> SplitIntoFragmentsAsync(
    PairwiseFirstPointAltContext context,
    IReadOnlyList<PointModel> points)
    => SplitIntoFragmentsAsync(
      points,
      context.HigherLimit,
      (firstPoint, secondPoint) => MeasureCompensatedResistanceAsync(context, firstPoint, secondPoint));

  /// <summary>
  /// Рекурсивно разбивает точки на связные фрагменты относительно первой точки текущего набора.
  /// </summary>
  /// <param name="points">Точки локализуемой части цепи.</param>
  /// <param name="upperBound">Верхняя допустимая граница сопротивления.</param>
  /// <param name="measureAsync">Функция измерения сопротивления между двумя точками.</param>
  /// <returns>Связные фрагменты и первое значение выше верхней границы.</returns>
  internal static async Task<LocalizationResult> SplitIntoFragmentsAsync(
    IReadOnlyList<PointModel> points,
    double upperBound,
    Func<PointModel, PointModel, Task<double>> measureAsync)
  {
    if (points.Count == 0)
    {
      return new LocalizationResult([], null);
    }

    if (points.Count == 1)
    {
      return new LocalizationResult([new ChainModel([points[0]])], null);
    }

    var connectedPoints = new List<PointModel> { points[0] };
    var unresolvedPoints = new List<PointModel>();
    double? firstAboveUpperBound = null;

    foreach (var point in points.Skip(1))
    {
      var resistance = await measureAsync(points[0], point);
      if (IsAboveUpperBound(resistance, upperBound))
      {
        firstAboveUpperBound ??= resistance;
        unresolvedPoints.Add(point);
      }
      else
      {
        connectedPoints.Add(point);
      }
    }

    var fragments = new List<ChainModel> { new(connectedPoints) };
    if (unresolvedPoints.Count > 0)
    {
      var nested = await SplitIntoFragmentsAsync(unresolvedPoints, upperBound, measureAsync);
      fragments.AddRange(nested.Fragments);
      firstAboveUpperBound ??= nested.FirstAboveUpperBound;
    }

    return new LocalizationResult(fragments, firstAboveUpperBound);
  }

  /// <summary>
  /// Измеряет сопротивление между точками с компенсацией сопротивлений контактов и кабеля.
  /// </summary>
  /// <param name="context">Контекст выполнения проверки ЭТ.</param>
  /// <param name="firstPoint">Первая точка измеряемого участка.</param>
  /// <param name="secondPoint">Вторая точка измеряемого участка.</param>
  /// <returns>Скомпенсированное сопротивление между точками.</returns>
  private static async Task<double> MeasureCompensatedResistanceAsync(
    PairwiseFirstPointAltContext context,
    PointModel firstPoint,
    PointModel secondPoint)
  {
    var messageService = context.MessageService;
    await CommandMessages.PublishPointsCheckHeaderAsync(
      messageService,
      firstPoint,
      secondPoint,
      CircuitFaultType.OpenCircuit);

    try
    {
      await ConnectToBothBusesAsync(firstPoint, messageService);
      var firstPointResistance = await MeasureAsync(context);
      await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusAAsync(
        firstPoint,
        messageService,
        context.IsPolarityReversed);

      await ConnectToBothBusesAsync(secondPoint, messageService);
      var secondPointResistance = await MeasureAsync(context);
      await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusBAsync(
        secondPoint,
        messageService,
        context.IsPolarityReversed);

      var measurementTarget = $"{EquipmentService.GetPointKey(firstPoint)},{EquipmentService.GetPointKey(secondPoint)}";
      var measurement = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var rawResistance = await MeasureAsync(context);
        var resistance = PairwiseFirstPointCheckerAlt.CalculateFinalResistance(
          rawResistance,
          firstPointResistance,
          secondPointResistance,
          context.LowerLimit,
          context.HigherLimit,
          context.CabelResistance);
        var connected = !IsAboveUpperBound(resistance, context.HigherLimit);

        await MeasurementMessages.PublishResultAsync(
          CheckType.ControlProgram,
          ResistanceUnit.Ohm,
          new MeasurementRange(resistance, context.LowerLimit, context.HigherLimit),
          connected,
          measurementTarget,
          outputService: messageService);

        return (connected, resistance);
      }, messageService, measurementTask: true);

      return measurement.Answer;
    }
    finally
    {
      await DisconnectPointFromBothBusesAsync(firstPoint, context);
      await DisconnectPointFromBothBusesAsync(secondPoint, context);
    }
  }

  /// <summary>
  /// Подключает точку одновременно к шинам A и B.
  /// </summary>
  /// <param name="point">Подключаемая точка.</param>
  /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
  /// <returns>Задача, представляющая подключение точки.</returns>
  private static async Task ConnectToBothBusesAsync(
    PointModel point,
    IUserInteractionService messageService)
  {
    var module = EquipmentService.GetModuleByPoint(point);
    await module.PointManager.ConnectRelayAsync(
      BusPoint.AB,
      point.PointNumber,
      messageService);
  }

  /// <summary>
  /// Отключает точку от шин A и B.
  /// </summary>
  /// <param name="point">Отключаемая точка.</param>
  /// <param name="context">Контекст выполнения проверки ЭТ.</param>
  /// <returns>Задача, представляющая отключение точки.</returns>
  private static async Task DisconnectPointFromBothBusesAsync(
    PointModel point,
    PairwiseFirstPointAltContext context)
  {
    await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusAAsync(
      point,
      context.MessageService,
      context.IsPolarityReversed);
    await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusBAsync(
      point,
      context.MessageService,
      context.IsPolarityReversed);
  }

  /// <summary>
  /// Считывает сопротивление мультиметром в режиме прозвонки.
  /// </summary>
  /// <param name="context">Контекст выполнения проверки ЭТ.</param>
  /// <returns>Измеренное сопротивление.</returns>
  private static async Task<double> MeasureAsync(PairwiseFirstPointAltContext context)
  {
    var meter = await EquipmentService.GetFastMeterOrThrow(context.MessageService);
    return await meter.ContinuityManager.CheckContinuityAsync(
      new MeasurementRange(context.Value, context.LowerLimit, context.HigherLimit),
      context.MessageService);
  }

  /// <summary>
  /// Проверяет, требует ли измерение локализации по верхней границе.
  /// </summary>
  /// <param name="resistance">Измеренное сопротивление.</param>
  /// <param name="upperBound">Верхняя допустимая граница сопротивления.</param>
  /// <returns>
  /// <see langword="true"/>, если сопротивление выше верхней границы или измеритель вернул перегрузку.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool IsAboveUpperBound(double resistance, double upperBound)
    => MeasurementValueFormatter.IsOverloadValue(resistance) || resistance > upperBound;

  /// <summary>
  /// Содержит результат рекурсивной локализации цепи ЭТ.
  /// </summary>
  /// <param name="Fragments">Связные фрагменты исходной части цепи.</param>
  /// <param name="FirstAboveUpperBound">Первое значение выше верхней границы.</param>
  internal sealed record LocalizationResult(
    List<ChainModel> Fragments,
    double? FirstAboveUpperBound);
}
