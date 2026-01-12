using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using System.Linq;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies
{
  internal static class NodeFullChecker
  {
    internal delegate Task<(bool Result, double Value)> PerformMeasurementAsync(double value, IUserInteractionService userMessageService, CancellationToken cancellationToken, VoltageEnum.Type typeVoltage = VoltageEnum.Type.ACW);

    static private List<ChainModel> ErrorsPoints = new List<ChainModel>();

    /// <summary>
    /// Выполняет последовательную проверку точек с накоплением на одной из них (узел).
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task<List<ShowMessageModel>> CheckSequenceAsync(NodeFullContext context)
    {
      List<ShowMessageModel> ErrorMessage = new List<ShowMessageModel>();

      var pointsList = context.SchemeModel.GetPointsDisconnected();
      if (pointsList.Count == 0)
      {
        return ErrorMessage;
      }
      ErrorsPoints = new List<ChainModel>();

      await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.FullNode));

      foreach (var point in pointsList)
      {
        var chainModels = new ChainModel(point);
        context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();
        await DeviceManager.ConnectChainToBusBAsync(chainModels, context.MessageService);
      }

      foreach (var point in pointsList)
      {
        context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();
        var chainModels = new ChainModel(point);


        await context.MessageService.ShowMessageAsync(new ShowMessageModel($"Проверка {chainModels.ToString()}"), IsBlockStart: true);

        await DeviceManager.SwitchChainFromBusBToAAsync(chainModels, context.MessageService);

        var answer = await context.PerformMeasurementAsync(context.Value, context.MessageService, context.MessageService.GetCancellationToken());

        if (!answer.Result)
        {
          context.CommandManager.AddErrorMethod(context.CommandModel.PointErrors.NodeExecutePointError($"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}", PointModel.ConvertToPointStrings(chainModels.PointModels), $"{answer.Value} МОм (>{context.Value} МОм)", context.CommandModel.StartLineNumber, context.CommandModel.FormattedStartLineNumber));
          ErrorsPoints.Add(chainModels);
        }

        await DeviceManager.SwitchChainFromBusAToBAsync(chainModels, context.MessageService);
      }

      if (ErrorsPoints.Count > 0)
      {
        await context.MessageService.ShowMessageAsync(new ShowMessageModel($"Бракованные точки"), IsBlockStart: true);
        foreach (var point in ErrorsPoints)
        {
          await context.MessageService.ShowMessageAsync(new ShowMessageModel($"Найден брак при проверке цепи", message: point.ToString(), type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, IsBlockStart: true);
        }

        await context.MessageService.ShowMessageAsync(new ShowMessageModel("Анализ на наличие короткого замыкания между точками"), IsBlockStart: true);
        var chains = await FindAllShortCircuitChainsAsync(context.PerformMeasurementAsync, ErrorsPoints, context.Value, context.MessageService);


        await context.MessageService.ShowMessageAsync(
           new ShowMessageModel($"Результаты проверки")
           { IndentLevel = 1 });

        foreach (var chain in chains)
        {
          var chainStr = await PointFormater.GetFormatDisconnectPoint(chain.Chain);

          context.CommandManager.AddErrorMethod(context.CommandModel.PointErrors.ChainError($"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}", chainStr, context.CommandModel.StartLineNumber, context.CommandModel.FormattedStartLineNumber));

          var err = ExecutorMessageBuilder.BuildMeasurementResultMessage(context.TypeCommand, context.LowerLimit, context.HigherLimit, chain.Value, chainStr);
          err.Status = ShowMessageModel.MessageType.Error;
          err.IndentLevel = 3;
          //var err = new ShowMessageModel($"{chainStr}", message: $"Rизм = {chain.Item2}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 };
          ErrorMessage.Add(err);
          await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {err.ToString()}"));
        }
      }

      return ErrorMessage;
    }

    /// <summary>
    /// Делит список точек пополам.
    /// Если количество нечётное — первая часть будет на один элемент больше.
    /// </summary>
    /// <param name="points">Список точек.</param>
    /// <returns>Кортеж из двух списков: левая и правая половины.</returns>
    public static (List<PointModel> Left, List<PointModel> Right) SplitInHalf(List<PointModel> points)
    {
      int middle = (points.Count + 1) / 2;
      var left = points.Take(middle).ToList();
      var right = points.Skip(middle).ToList();
      return (left, right);
    }

    private static async Task DisconnectAllFromBusBAsync(List<ChainModel> points, IUserInteractionService messageService)
    {
      foreach (var point in points)
      {
        await DeviceManager.DisconnectChainFromBusBAsync(point, messageService);
      }
    }

    /// <summary>
    /// Ищет все цепи КЗ среди списка бракованных точек с минимальным числом измерений.
    /// </summary>
    /// <param name="faultyPoints">Список бракованных точек.</param>
    /// <param name="resistance">Порог сопротивления для определения КЗ.</param>
    /// <param name="messageService">Сервис сообщений.</param>
    /// <returns>Список цепей (каждая цепь — список связанных точек).</returns>
    public static async Task<List<(List<ChainModel> Chain, double Value)>> FindAllShortCircuitChainsAsync(
      PerformMeasurementAsync performMeasurementAsync,
        List<ChainModel> faultyPoints,
        double resistance,
        IUserInteractionService messageService)
    {
      var chains = new List<(List<ChainModel>, double)>();
      var visited = new HashSet<ChainModel>();

      foreach (var point in faultyPoints)
      {
        if (visited.Contains(point))
          continue;

        var chain = await FindChainAsync(performMeasurementAsync, point, faultyPoints, resistance, messageService, visited);

        if (chain.Item1.Count > 1)
          chains.Add(chain);
      }

      return chains;
    }

    /// <summary>
    /// Для заданной стартовой точки ищет всю цепь КЗ (все связанные с ней точки) методом BFS.
    /// </summary>
    /// <param name="start">Стартовая точка.</param>
    /// <param name="allPoints">Список всех бракованных точек.</param>
    /// <param name="resistance">Порог сопротивления для определения КЗ.</param>
    /// <param name="messageService">Сервис сообщений.</param>
    /// <param name="visited">Множество уже посещённых точек (будет обновлено).</param>
    /// <returns>Список всех точек, связанных с данной через КЗ.</returns>
    private static async Task<(List<ChainModel>, double)> FindChainAsync(
      PerformMeasurementAsync performMeasurementAsync,
        ChainModel start,
        List<ChainModel> allPoints,
        double resistance,
        IUserInteractionService messageService,
        HashSet<ChainModel> visited)
    {
      var queue = new Queue<ChainModel>();
      var chain = new List<ChainModel>();
      var result = (new List<ChainModel>(), new double());

      List<double> answers = new List<double>();

      queue.Enqueue(start);
      visited.Add(start);
      chain.Add(start);

      while (queue.Count > 0)
      {
        var current = queue.Dequeue();

        foreach (var candidate in allPoints)
        {
          if (visited.Contains(candidate) || candidate.Equals(current))
            continue;

          var isConnected = await IsShortCircuitedAsync(performMeasurementAsync, current, candidate, resistance, messageService);
          if (isConnected.Connected)
          {
            queue.Enqueue(candidate);
            visited.Add(candidate);
            chain.Add(candidate);
            answers.Add(isConnected.Vaue);
          }
        }
      }

      result = (chain, answers.Average());
      return result;
    }

    /// <summary>
    /// Проверяет, замкнуты ли две точки между собой.
    /// </summary>
    private static async Task<(bool Connected, double Vaue)> IsShortCircuitedAsync(PerformMeasurementAsync performMeasurementAsync, ChainModel a, ChainModel b, double resistance, IUserInteractionService messageService)
    {
      var allPoints = ErrorsPoints;
      await DisconnectAllFromBusAAsync(allPoints, messageService);
      await DisconnectAllFromBusBAsync(allPoints, messageService);

      await DeviceManager.ConnectChainToBusAAsync(a, messageService);
      await DeviceManager.ConnectChainToBusBAsync(b, messageService);

      var anwer = await performMeasurementAsync(resistance, messageService, messageService.GetCancellationToken());

      var result = (!anwer.Result, anwer.Value);

      await DeviceManager.DisconnectChainFromBusAAsync(a, messageService);
      await DeviceManager.DisconnectChainFromBusBAsync(b, messageService);

      return result;
    }

    private static async Task DisconnectAllFromBusAAsync(List<ChainModel> points, IUserInteractionService messageService)
    {
      foreach (var point in points)
      {
        await DeviceManager.DisconnectChainFromBusAAsync(point, messageService);
      }
    }
  }
}
