using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
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
    static public async Task<List<ShowMessageModel>> CheckSequenceAsync(NodeAccumulationContext context)
    {
      List<ShowMessageModel> ErrorMessage = new List<ShowMessageModel>();
      List<(ChainModel, ChainModel)> errorChains = new List<(ChainModel, ChainModel)>();

      var groupChains = context.SchemeModel.GetPointsDisconnected();
      if (groupChains.ChainModels.Count == 0)
      {
        return ErrorMessage;
      }

      var messageService = context.MessageService;
      var cancellationToken = messageService.GetCancellationToken();

      await messageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.AccumulatingNode, context.IsPolarityReversed));

      foreach (var chain in groupChains.ChainModels)
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();

        var str = string.Empty;
        foreach (var point in chain.PointModels)
        {
          str += $"{EquipmentService.GetPointKey(point)},";
        }
        str = str.Remove(str.Length - 1);

        await messageService.ShowMessageAsync(ExecutorMessageBuilder.BuildChainCheckBlock(str), IsBlockStart: true);

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
            var strError = await PointFormater.GetFormatDisconnectPoint(new List<ChainModel>() { chain, localized });
            errorChains.Add((chain, localized));

            var err = ExecutorMessageBuilder.BuildMeasurementResultMessage(context.TypeCommand, context.LowerLimit, context.HigherLimit, measured.Value, strError);
            err.Status = ShowMessageModel.MessageType.Error;
            err.IndentLevel = 3;

            await messageService.ShowMessageAsync(err);

            if (context.CommandModel.PointErrors != null)
            {
              context.CommandManager.AddErrorMethod(
                context.CommandModel.PointErrors.ChainPairError($"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}",
                PointModel.ConvertToPointStrings(chain.PointModels),
                PointModel.ConvertToPointStrings(localized.PointModels),
                measured.Value.ToString(),
                messageService.GetLastLineNumber(),
                context.CommandModel.FormattedStartLineNumber));
            }

            ErrorMessage.Add(err);
            await messageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {err.ToString()}"));

          }
          else
          {
            await messageService.ShowMessageAsync(new ShowMessageModel("Локализация не удалась", message: "Не удалось точно определить неисправную цепь", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
            ErrorMessage.Add(new ShowMessageModel($"Ошибка локализации", message: $"Не удалось точно определить замыкание цепей", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
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

      return ErrorMessage;
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
      try
      {
        ChainModel errorPoint = null;
        step++;

        await messageService.ShowMessageAsync(new ShowMessageModel($"Выполенение шага {step}"));
        var (leftPart, rightPart) = SplitInHalf(candidates);

        await messageService.ShowMessageAsync(new ShowMessageModel("Отключение левой части группы точек"));
        await DeviceManager.RelayModule.GroupManager.DisconnectAllPointFromBusBAsync(leftPart, messageService, revers);

        IRelaySwitchModule module = null;
        if (leftPart.ChainModels.FirstOrDefault() != null)
        {
          module = EquipmentService.GetModuleByPoint(leftPart.ChainModels.FirstOrDefault().PointModels.FirstOrDefault());
        }
        else if (rightPart.ChainModels.FirstOrDefault() != null)
        {
          module = EquipmentService.GetModuleByPoint(rightPart.ChainModels.FirstOrDefault().PointModels.FirstOrDefault());
        }

        (bool Result, double Value) measured;

        if (module != null)
        {
          measured = await performMeasurementAsync(resistance, messageService, cancellationToken, module.SwitchResistance, type: type);
        }
        else
        {
          measured = await performMeasurementAsync(resistance, messageService, cancellationToken, 0, type: type);
        }

        if (!measured.Result)
        {
          if (rightPart.ChainModels.Count > 1)
          {
            errorPoint = await LocalizeFaultyPointAsync(performMeasurementAsync, rightPart, resistance, messageService, cancellationToken, type, revers);
          }
          else
          {
            errorPoint = rightPart.ChainModels[0];
            return errorPoint;
          }
        }
        else
        {
          await messageService.ShowMessageAsync(new ShowMessageModel("Отключение правой части группы точек"));
          await DeviceManager.RelayModule.GroupManager.DisconnectAllPointFromBusBAsync(rightPart, messageService, revers);

          await messageService.ShowMessageAsync(new ShowMessageModel("Подключение левой части группы точек"));
          await DeviceManager.RelayModule.GroupManager.ConnectAllFromBusBAsync(leftPart, messageService, revers);

          if (leftPart.ChainModels.Count > 1)
          {
            errorPoint = await LocalizeFaultyPointAsync(performMeasurementAsync, leftPart, resistance, messageService, cancellationToken, type, revers);
          }
          else
          {
            measured = await performMeasurementAsync(resistance, messageService, cancellationToken, module.SwitchResistance, type: type);
            if (!measured.Result)
            {
              errorPoint = leftPart.ChainModels[0];
              return errorPoint;
            }
            else
            {
              return errorPoint;
            }
          }
        }

        await DeviceManager.RelayModule.GroupManager.ConnectAllFromBusBAsync(candidates, messageService, revers);
        return errorPoint;
      }
      catch
      {
        return null;
      }
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
