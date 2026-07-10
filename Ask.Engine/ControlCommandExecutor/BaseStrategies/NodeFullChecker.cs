using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
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
  internal static class NodeFullChecker
  {
    internal delegate Task<(bool Result, double Value)> PerformMeasurementAsync(double value, IUserInteractionService userMessageService, CancellationToken cancellationToken, double errorResistance, VoltageEnum.Type typeVoltage = VoltageEnum.Type.ACW);

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
      List<ChainModel> errorChains = new();

      var groupChains = context.SchemeModel.GetPointsDisconnected();
      if (groupChains.ChainModels.Count == 0)
      {
        return ErrorMessage;
      }
      ErrorsPoints = new List<ChainModel>();

      if (ProtocolConfig.GetTestStepMessagesInProtocol())
      { 
        await context.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.FullNode, context.IsPolarityReversed));
      }

      foreach (var chainModels in groupChains.ChainModels)
      {
        context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();
        await DeviceManager.RelayModule.ChainManager.ConnectChainToBusBAsync(chainModels, context.MessageService, context.IsPolarityReversed);
      }

      foreach (var chainModels in groupChains.ChainModels)
      {
        context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();

        await context.MessageService.ShowMessageAsync(new ShowMessageModel($"Проверка {chainModels.ToString()}"), IsBlockStart: true);

        await DeviceManager.RelayModule.ChainManager.SwitchChainFromBusBToAAsync(chainModels, context.MessageService, context.IsPolarityReversed);

        var answer = await context.PerformMeasurementAsync(context.Value, context.MessageService, context.MessageService.GetCancellationToken(), context.InternalResistance, context.VoltageType);

        if (!answer.Result)
        {
          context.CommandManager.AddErrorMethod(
            context.CommandModel.PointErrors.NodeExecutePointError($"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}", 
            PointModel.ConvertToPointStrings(chainModels.PointModels), 
            $"{answer.Value} МОм (>{context.Value} МОм)",
             context.MessageService.GetLastLineNumber(), 
            context.CommandModel.FormattedStartLineNumber));
          ErrorsPoints.Add(chainModels);
        }

        await DeviceManager.RelayModule.ChainManager.SwitchChainFromBusAToBAsync(chainModels, context.MessageService, context.IsPolarityReversed);
      }

      if (ErrorsPoints.Count > 0)
      {
        await context.MessageService.ShowMessageAsync(new ShowMessageModel($"Бракованные точки"), IsBlockStart: true);
        foreach (var point in ErrorsPoints)
        {
          await context.MessageService.ShowMessageAsync(new ShowMessageModel($"Найден брак при проверке цепи", message: point.ToString(), type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, IsBlockStart: true);
        }

        await context.MessageService.ShowMessageAsync(new ShowMessageModel("Анализ на наличие короткого замыкания между точками"), IsBlockStart: true);
        var chains = await FindAllShortCircuitChainsAsync(context.PerformMeasurementAsync, ErrorsPoints, context.Value, context.MessageService, context.VoltageType, context.IsPolarityReversed);

        foreach (var chain in chains)
        {
          var chainStr = await PointFormater.GetFormatDisconnectPoint(chain.Chain);
          context.CommandManager.AddErrorMethod(
            context.CommandModel.PointErrors.ChainError(
              $"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}", 
              chainStr, 
              context.MessageService.GetLastLineNumber(), 
              context.CommandModel.FormattedStartLineNumber));
          errorChains.AddRange(chain.Chain);

          var err = await FaultChainMeasurementService.MeasureAsync(
            context,
            chain.Chain,
            chainStr,
            (value, service, token, resistance, type) => context.PerformMeasurementAsync(value, service, token, resistance, type),
            context.VoltageType);

          ErrorMessage.Add(err);
          await context.MessageService.ShowMessageAsync(new ShowMessageModel(debug: $"Добавлена ошибка: {err.ToString()}"));
        }
      }

      if (context.IsInvokedByAnotherCommand)
      {
        context.SchemeModel.SetErrorChainDisconnectedPoints(errorChains);
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

    private static async Task DisconnectAllFromBusBAsync(List<ChainModel> points, IUserInteractionService messageService, bool revers)
    {
      foreach (var point in points)
      {
        await DeviceManager.RelayModule.ChainManager.DisconnectChainFromBusBAsync(point, messageService, revers);
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
        IUserInteractionService messageService, VoltageEnum.Type typeVoltage, bool revers)
    {
      var chains = new List<(List<ChainModel>, double)>();
      var visited = new HashSet<ChainModel>();

      foreach (var point in faultyPoints)
      {
        if (visited.Contains(point))
          continue;

        var chain = await FindChainAsync(performMeasurementAsync, point, faultyPoints, resistance, messageService, visited, typeVoltage, revers);

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
        HashSet<ChainModel> visited, VoltageEnum.Type typeVoltage, bool revers)
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

          var isConnected = await IsShortCircuitedAsync(performMeasurementAsync, current, candidate, resistance, messageService, typeVoltage, revers);
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
    private static async Task<(bool Connected, double Vaue)> IsShortCircuitedAsync(PerformMeasurementAsync performMeasurementAsync, ChainModel a, ChainModel b, double resistance, IUserInteractionService messageService, VoltageEnum.Type typeVoltage, bool revers)
    {
      var allPoints = ErrorsPoints;
      await DisconnectAllFromBusAAsync(allPoints, messageService, revers);
      await DisconnectAllFromBusBAsync(allPoints, messageService, revers);

      await DeviceManager.RelayModule.ChainManager.ConnectChainToBusAAsync(a, messageService, revers);
      await DeviceManager.RelayModule.ChainManager.ConnectChainToBusBAsync(b, messageService, revers);

      var module = EquipmentService.GetModuleByPoint(a.PointModels.FirstOrDefault());
      var anwer = await performMeasurementAsync(resistance, messageService, messageService.GetCancellationToken(), module.SwitchResistance);

      var result = (!anwer.Result, anwer.Value);

      await DeviceManager.RelayModule.ChainManager.DisconnectChainFromBusAAsync(a, messageService, revers);
      await DeviceManager.RelayModule.ChainManager.DisconnectChainFromBusBAsync(b, messageService, revers);

      return result;
    }

    private static async Task DisconnectAllFromBusAAsync(List<ChainModel> points, IUserInteractionService messageService, bool revers)
    {
      foreach (var point in points)
      {
        await DeviceManager.RelayModule.ChainManager.DisconnectChainFromBusAAsync(point, messageService, revers);
      }
    }
  }
}
