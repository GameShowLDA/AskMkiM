using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies;

/// <summary>
/// Локализует участки цепи ЭТ, сопротивление которых превышает верхнюю границу.
/// </summary>
internal static class EhtHighResistanceLocalizationService
{
  /// <summary>
  /// Локализует участки исходной цепи с сопротивлением выше верхней границы.
  /// </summary>
  /// <param name="context">Контекст выполнения проверки ЭТ.</param>
  /// <param name="sourceChain">Исходная цепь проверяемых точек.</param>
  /// <param name="initialAboveUpperBound">Первое значение выше верхней границы в основном прогоне.</param>
  /// <returns>Ошибки, сформированные по результатам локализации.</returns>
  internal static async Task<AlgorithmExecutionResult> LocalizeAsync(
    PairwiseFirstPointAltContext context,
    ChainModel sourceChain,
    double initialAboveUpperBound)
  {
    var result = new AlgorithmExecutionResult();
    if (sourceChain.PointModels.Count < 2)
    {
      return result;
    }

    await WaitForExecutionAsync(context.MessageService);
    await ExecutionMessages.PublishLocalizationHeaderAsync(context.MessageService);
    var localization = await SplitIntoFragmentsAsync(context, sourceChain.PointModels);
    await WaitForExecutionAsync(context.MessageService);
    var localized = localization.Fragments.Count >= 2 && localization.FirstAboveUpperBound.HasValue;
    var errorChains = localized
      ? localization.Fragments
      : [sourceChain];
    var errorValue = localized
      ? localization.FirstAboveUpperBound!.Value
      : initialAboveUpperBound;
    var display = await FormatLocalizedBreaksAsync(errorChains);
    var error = MeasurementMessages.BuildMeasurementResultMessage(
      ResistanceUnit.Ohm,
      new MeasurementRange(
        errorValue,
        context.LowerLimit,
        context.HigherLimit),
      false,
      display);

    await MeasurementMessages.PublishBuiltMessageAsync(CheckType.ControlProgram, error, context.MessageService);
    result.Errors.Add(error);
    context.CommandManager.AddErrorMethod(
      context.CommandModel.PointErrors.DisconnectChainError(
        $"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}",
        display,
        MeasurementValueFormatter.FormatWithUnit(errorValue, "Ом"),
        context.CommandModel.StartLineNumber,
        context.CommandModel.FormattedStartLineNumber));

    return result;
  }

  /// <summary>
  /// Разбивает точки на связные фрагменты с использованием оборудования из контекста ЭТ.
  /// </summary>
  /// <param name="context">Контекст выполнения проверки ЭТ.</param>
  /// <param name="points">Точки исходной цепи.</param>
  /// <returns>Результат локализации цепи.</returns>
  private static async Task<LocalizationResult> SplitIntoFragmentsAsync(
    PairwiseFirstPointAltContext context,
    IReadOnlyList<PointModel> points)
    => await SplitIntoFragmentsAsync(
      points,
      context.HigherLimit,
      (firstPoint, secondPoint) => MeasureCompensatedResistanceAsync(context, firstPoint, secondPoint));

  /// <summary>
  /// Рекурсивно разбивает точки на фрагменты по результатам измерений относительно первой точки.
  /// </summary>
  /// <param name="points">Точки локализуемого фрагмента.</param>
  /// <param name="upperBound">Верхняя допустимая граница сопротивления.</param>
  /// <param name="measureAsync">Функция измерения сопротивления между двумя точками.</param>
  /// <returns>Фрагменты цепи и первое значение выше верхней границы.</returns>
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

    var connected = new List<PointModel> { points[0] };
    var aboveUpperBound = new List<PointModel>();
    double? firstAboveUpperBound = null;

    foreach (var point in points.Skip(1))
    {
      var resistance = await measureAsync(points[0], point);
      if (IsAboveUpperBound(resistance, upperBound))
      {
        firstAboveUpperBound ??= resistance;
        aboveUpperBound.Add(point);
      }
      else
      {
        connected.Add(point);
      }
    }

    var fragments = new List<ChainModel> { new(connected) };
    if (aboveUpperBound.Count > 0)
    {
      var nested = await SplitIntoFragmentsAsync(aboveUpperBound, upperBound, measureAsync);
      fragments.AddRange(nested.Fragments);
      firstAboveUpperBound ??= nested.FirstAboveUpperBound;
    }

    return new LocalizationResult(fragments, firstAboveUpperBound);
  }

  /// <summary>
  /// Измеряет сопротивление между точками с компенсацией контактных сопротивлений и сопротивления кабеля.
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
    var service = context.MessageService;
    double result;
    try
    {
      await WaitForExecutionAsync(service);
      await ConnectToBothBusesAsync(firstPoint, service);

      await WaitForExecutionAsync(service);
      var firstResistance = await MeasureAsync(context);

      await WaitForExecutionAsync(service);
      await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusAAsync(firstPoint, service, context.IsPolarityReversed);

      await WaitForExecutionAsync(service);
      await ConnectToBothBusesAsync(secondPoint, service);

      await WaitForExecutionAsync(service);
      var secondResistance = await MeasureAsync(context);

      await WaitForExecutionAsync(service);
      await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusBAsync(secondPoint, service, context.IsPolarityReversed);

      await WaitForExecutionAsync(service);
      var pairResistance = await MeasureAsync(context);
      result = PairwiseFirstPointCheckerAlt.CalculateFinalResistance(
        pairResistance,
        firstResistance,
        secondResistance,
        context.LowerLimit,
        context.HigherLimit,
        context.CabelResistance);
    }
    finally
    {
      await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusAAsync(firstPoint, service, context.IsPolarityReversed);
      await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusBAsync(firstPoint, service, context.IsPolarityReversed);
      await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusAAsync(secondPoint, service, context.IsPolarityReversed);
      await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusBAsync(secondPoint, service, context.IsPolarityReversed);
    }

    await WaitForExecutionAsync(service);
    await PublishIntermediateMeasurementAsync(context, firstPoint, secondPoint, result);
    return result;
  }

  /// <summary>
  /// Ожидает продолжения локализации и проверяет запрос на её остановку.
  /// </summary>
  /// <param name="service">Сервис взаимодействия с пользователем.</param>
  /// <returns>Задача, представляющая ожидание разрешения на продолжение.</returns>
  /// <exception cref="OperationCanceledException">
  /// Выбрасывается, если запрошена остановка выполнения.
  /// </exception>
  internal static async Task WaitForExecutionAsync(IUserInteractionService service)
  {
    var cancellationToken = service.GetCancellationToken();
    cancellationToken.ThrowIfCancellationRequested();
    await service.WaitIfPausedAsync();
    cancellationToken.ThrowIfCancellationRequested();
  }

  /// <summary>
  /// Формирует результат локализации, сохраняя звёздочки на границах разрывов цепи.
  /// </summary>
  /// <param name="fragments">Связные фрагменты локализованной цепи.</param>
  /// <returns>Последовательность фрагментов, каждый из которых ограничен звёздочками.</returns>
  internal static async Task<string> FormatLocalizedBreaksAsync(IReadOnlyList<ChainModel> fragments)
  {
    var formattedFragments = new List<string>(fragments.Count);
    foreach (var fragment in fragments)
    {
      formattedFragments.Add(await PointFormater.GetFormatDisconnectPoint([fragment]));
    }

    return string.Concat(formattedFragments);
  }

  /// <summary>
  /// Публикует промежуточный результат измерения пары точек при локализации.
  /// </summary>
  /// <param name="context">Контекст выполнения проверки ЭТ.</param>
  /// <param name="firstPoint">Первая точка измеряемого участка.</param>
  /// <param name="secondPoint">Вторая точка измеряемого участка.</param>
  /// <param name="resistance">Скомпенсированное сопротивление между точками.</param>
  internal static async Task PublishIntermediateMeasurementAsync(
    PairwiseFirstPointAltContext context,
    PointModel firstPoint,
    PointModel secondPoint,
    double resistance)
  {
    var resultExecutor = context.ResultMessageExecutor
      ?? throw new InvalidOperationException(
        "Не задан исполнитель сообщений результатов измерения.");
    var measurementTarget = $"{FormatPoint(firstPoint)},{FormatPoint(secondPoint)}";

    await resultExecutor.PublishMeasurementResultAsync(
      new MeasurementResultMessageContext(
        context.TypeCommand,
        new MeasurementRange(resistance, context.LowerLimit, context.HigherLimit),
        context.MessageService,
        measurementTarget)
      {
        IsIntermediate = true,
      });
  }

  /// <summary>
  /// Формирует обозначение точки для результата локализации.
  /// </summary>
  /// <param name="point">Точка измеряемого участка.</param>
  /// <returns>Мнемоника точки с машинным адресом, если его отображение включено.</returns>
  private static string FormatPoint(PointModel point)
  {
    if (!DeviceDisplayConfig.GetMachineAddressVisibility())
    {
      return point.Mnemonic;
    }

    var address = ExecutionConfig.GetIsLegacyCompatibilityModeEnabled()
      ? LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(point.ToString())
      : point.ToString();
    return $"{point.Mnemonic}[{address}]";
  }

  /// <summary>
  /// Подключает точку одновременно к шинам A и B.
  /// </summary>
  /// <param name="point">Подключаемая точка.</param>
  /// <param name="service">Сервис взаимодействия с пользователем.</param>
  /// <returns>Задача, представляющая подключение точки.</returns>
  private static async Task ConnectToBothBusesAsync(
    PointModel point,
    IUserInteractionService service)
  {
    var module = EquipmentService.GetModuleByPoint(point);
    await module.PointManager.ConnectRelayAsync(BusPoint.AB, point.PointNumber, service);
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
  /// <param name="Fragments">Связные фрагменты исходной цепи.</param>
  /// <param name="FirstAboveUpperBound">Первое измеренное значение выше верхней границы.</param>
  internal sealed record LocalizationResult(
    List<ChainModel> Fragments,
    double? FirstAboveUpperBound);
}
