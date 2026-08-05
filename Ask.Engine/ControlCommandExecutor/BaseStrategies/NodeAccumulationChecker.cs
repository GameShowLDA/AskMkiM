using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies
{
  /// <summary>
  /// Класс для управления методом накапливающего узла.
  /// </summary>
  static internal class NodeAccumulationChecker
  {
    /// <summary>
    /// Делегат для выполнения измерений.
    /// </summary>
    /// <param name="value">Ожидаемое значение.</param>
    /// <param name="userMessageService">Элемент управления для вывода сообщений.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    internal delegate Task<(bool Result, double Value)> PerformMeasurementAsync(double value, IUserInteractionService userMessageService, CancellationToken cancellationToken, double errorResistance, VoltageEnum.Type type = VoltageEnum.Type.DCW);
    static private int step = 0;

    /// <summary>
    /// Выполняет последовательную проверку точек с накоплением на одной из них (узел).
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task<AlgorithmExecutionResult> CheckSequenceAsync(NodeAccumulationContext context)
    {
      var executionResult = new AlgorithmExecutionResult();
      List<(ChainModel, ChainModel)> errorChains = new List<(ChainModel, ChainModel)>();

      var groupChains = context.SchemeModel.GetPointsDisconnected();
      if (groupChains.ChainModels.Count == 0)
      {
        return executionResult;
      }

      var messageService = context.MessageService;
      var cancellationToken = messageService.GetCancellationToken();

      await CommandMessages.PublishCheckBlockHeaderAsync(
        messageService,
        ControlCheckAlgorithm.AccumulatingNode,
        context.IsPolarityReversed);

      foreach (var chain in groupChains.ChainModels)
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();

        var str = string.Empty;
        foreach (var point in chain.PointModels)
        {
          str += $"{EquipmentService.GetPointKey(point)},";
        }
        str = str.Remove(str.Length - 1);

        await CommandMessages.PublishChainCheckBlockAsync(messageService, str);

        foreach (var point in chain.PointModels)
        {
          await DeviceManager.RelayModule.PointManager.ConnectPointToBusAAsync(point, messageService, context.IsPolarityReversed);
        }

        var measured = await context.PerformMeasurementAsync(context.Value, messageService, cancellationToken, context.InternalResistance, context.VoltageType);
        if (!measured.Result)
        {
          step = 0;
          var chains = EquipmentService.GetDisconnectChainsBefore(context.SchemeModel, chain);
          var localized = await LocalizeFaultyPointAsync(context.PerformMeasurementAsync, chains, context.Value, messageService, cancellationToken, context.VoltageType, context.IsPolarityReversed);
          if (localized != null)
          {
            var faultChain = new List<ChainModel>() { chain, localized };
            var strError = await PointFormater.GetFormatDisconnectPoint(faultChain);
            errorChains.Add((chain, localized));

            var faultResult = await FaultChainMeasurementService.MeasureAsync(
              context,
              faultChain,
              strError,
              (value, service, token, resistance, type) => context.PerformMeasurementAsync(value, service, token, resistance, type),
              context.VoltageType);

            var err = faultResult.Errors.Single();
            await MeasurementMessages.PublishBuiltMessageAsync(err, messageService);

            if (context.CommandModel.PointErrors != null)
            {
              context.CommandManager.AddErrorMethod(
                context.CommandModel.PointErrors.ChainPairError($"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}",
                PointModel.ConvertToPointStrings(chain.PointModels),
                PointModel.ConvertToPointStrings(localized.PointModels),
                err.Message,
                messageService.GetLastLineNumber(),
                context.CommandModel.FormattedStartLineNumber));
            }

            executionResult.AddRange(faultResult);
            await ExecutionMessages.PublishDebugAsync($"Добавлена ошибка: {err}", messageService);

          }
          else
          {
            await ExecutionMessages.PublishLocalizationFailureAsync(messageService);
            executionResult.Errors.Add(ExecutionMessages.BuildLocalizationError());
          }
        }

        foreach (var point in chain.PointModels)
        {
          await DeviceManager.RelayModule.PointManager.SwitchPointFromBusAToBAsync(point, messageService, context.IsPolarityReversed);
        }
      }

      foreach (var chains in groupChains.ChainModels)
      {
        foreach (var points in chains.PointModels)
        {
          await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusBAsync(points, messageService, context.IsPolarityReversed);
        }
      }

      if (context.IsInvokedByAnotherCommand)
      {
        context.SchemeModel.SetErrorChainDisconnectedPoints(errorChains);
      }

      return executionResult;
    }

    /// <summary>
    /// Локализует неисправную точку методом половинного деления.
    /// Одна точка остаётся на шине A (известная как бракованная), остальные проверяются на шине B.
    /// </summary>
    /// <param name="knownFaultPoint">Известная точка, оставляемая на шине A.</param>
    /// <param name="candidates">Кандидаты на локализацию на шине B.</param>
    /// <param name="resistance">Пороговое сопротивление для проверки.</param>
    /// <param name="messageService">Сервис сообщений.</param>
    /// <returns>Локализованная точка или null, если локализация не удалась.</returns>
    public static async Task<ChainModel?> LocalizeFaultyPointAsync(
        PerformMeasurementAsync performMeasurementAsync,
        GroupModel candidates,
        double resistance,
        IUserInteractionService messageService,
        CancellationToken cancellationToken,
        VoltageEnum.Type type,
        bool revers
        )
    {
      if (candidates == null || candidates.ChainModels.Count == 0)
      {
        return null;
      }

      if (candidates.ChainModels.Count == 1)
      {
        return candidates.ChainModels[0];
      }

      step++;
      await ExecutionMessages.PublishLocalizationStepAsync(step, messageService);

      var (leftPart, rightPart) = SplitInHalf(candidates);
      var switchResistance = GetSwitchResistance(candidates);

      try
      {
        await ExecutionMessages.PublishGroupPartOperationAsync(
          "Отключение левой части группы точек", messageService);
        await DeviceManager.RelayModule.GroupManager.DisconnectAllPointFromBusBAsync(leftPart, messageService, revers);

        var measuredWithoutLeft = await performMeasurementAsync(resistance, messageService, cancellationToken, switchResistance, type: type);
        if (!measuredWithoutLeft.Result)
        {
          return rightPart.ChainModels.Count == 1
            ? rightPart.ChainModels[0]
            : await LocalizeFaultyPointAsync(performMeasurementAsync, rightPart, resistance, messageService, cancellationToken, type, revers);
        }

        await ExecutionMessages.PublishGroupPartOperationAsync(
          "Отключение правой части группы точек", messageService);
        await DeviceManager.RelayModule.GroupManager.DisconnectAllPointFromBusBAsync(rightPart, messageService, revers);

        await ExecutionMessages.PublishGroupPartOperationAsync(
          "Подключение левой части группы точек", messageService);
        await DeviceManager.RelayModule.GroupManager.ConnectAllFromBusBAsync(leftPart, messageService, revers);

        var measuredWithoutRight = await performMeasurementAsync(resistance, messageService, cancellationToken, switchResistance, type: type);
        if (!measuredWithoutRight.Result)
        {
          return leftPart.ChainModels.Count == 1
            ? leftPart.ChainModels[0]
            : await LocalizeFaultyPointAsync(performMeasurementAsync, leftPart, resistance, messageService, cancellationToken, type, revers);
        }

        return null;
      }
      finally
      {
        await DeviceManager.RelayModule.GroupManager.ConnectAllFromBusBAsync(candidates, messageService, revers);
      }
    }

    private static double GetSwitchResistance(GroupModel candidates)
    {
      var point = candidates.ChainModels
        .SelectMany(chain => chain.PointModels)
        .FirstOrDefault();

      var module = point != null ? EquipmentService.GetModuleByPoint(point) : null;
      return module?.SwitchResistance ?? 0;
    }

    /// <summary>
    /// Делит список точек пополам.
    /// Если количество нечётное — первая часть будет на один элемент больше.
    /// </summary>
    /// <param name="points">Список точек.</param>
    /// <returns>Кортеж из двух списков: левая и правая половины.</returns>
    public static (GroupModel Left, GroupModel Right) SplitInHalf(GroupModel points)
    {
      int middle = (points.ChainModels.Count + 1) / 2;
      var left = new GroupModel(points.ChainModels.Take(middle).ToList());
      var right = new GroupModel(points.ChainModels.Skip(middle).ToList());
      return (left, right);
    }
  }
}
