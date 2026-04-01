using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using System.Text;
using static Ask.Core.Shared.Metadata.Static.DelegateManager;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies
{
  /// <summary>
  /// Класс <see cref="ConnectedPointChecker"/> предназначен для проверки электрической
  /// последовательности соединённых точек в схеме и выявления разрывов цепей.
  /// </summary>
  internal static class ConnectedPointChecker
  {
    /// <summary>
    /// Делегат, определяющий метод выполнения измерений.
    /// </summary>
    /// <param name="value">Заданное значение сопротивления для измерения.</param>
    /// <param name="userMessageService">Сервис для отображения сообщений пользователю.</param>
    /// <param name="cancellationToken">Токен отмены для управления асинхронной операцией.</param>
    /// <returns>
    /// Асинхронная операция, возвращающая <c>true</c>, если измерение прошло успешно,
    /// или <c>false</c>, если обнаружена ошибка.
    /// </returns>
    internal delegate Task<(bool Result, double Value)> PerformMeasurementAsync(double value, IUserInteractionService userMessageService, CancellationToken cancellationToken, double errorResistance);

    /// <summary>
    /// Асинхронно выполняет проверку соединённых точек в схеме, формируя новый список цепей (ССИРТ)
    /// с учётом обнаруженных разрывов.
    /// </summary>
    static public async Task<(List<ShowMessageModel> errorMessage, List<ShowMessageModel> infoMessage)> CheckSequenceAsync(ConnectedPointContext context, PreMeasurementDelegate preMeasurementDelegate = null)
    {
      var errors = new List<ShowMessageModel>();
      var infos = new List<ShowMessageModel>();
      var sourceGroups = GetSourceGroups(context);

      if (!HasGroupsToProcess(sourceGroups))
      {
        return (errors, infos);
      }

      await ShowCheckBlockHeaderAsync(context);

      var newGroups = await BuildCheckedGroupsAsync(sourceGroups, context, preMeasurementDelegate, errors, infos);
      context.NewScheme = new SchemeModel(newGroups);

      return (errors, infos);
    }

    /// <summary>
    /// Возвращает список групп исходной схемы или пустую коллекцию, если схема отсутствует.
    /// </summary>
    private static List<GroupModel> GetSourceGroups(ConnectedPointContext context) =>
      context.SchemeModel?.GroupModels ?? new List<GroupModel>();

    /// <summary>
    /// Проверяет, есть ли в схеме группы для обработки.
    /// </summary>
    private static bool HasGroupsToProcess(List<GroupModel> sourceGroups) => sourceGroups.Count > 0;

    /// <summary>
    /// Показывает заголовок общего блока проверки в зависимости от типа команды.
    /// </summary>
    private static Task ShowCheckBlockHeaderAsync(ConnectedPointContext context)
    {
      var algorithm = context.TypeCommand == MeasurementTypeCommand.KC
        ? ControlCheckAlgorithm.ResistanceRelativeToFirstPoint
        : ControlCheckAlgorithm.MessageRelativeToFirstPoint;

      return context.MessageService.ShowMessageAsync(
        ExecutorMessageBuilder.BuildCheckBlockHeader(algorithm, context.IsPolarityReversed));
    }

    /// <summary>
    /// Формирует новый набор групп после проверки всех цепей исходной схемы.
    /// </summary>
    private static async Task<List<GroupModel>> BuildCheckedGroupsAsync(
      List<GroupModel> sourceGroups,
      ConnectedPointContext context,
      PreMeasurementDelegate preMeasurementDelegate,
      List<ShowMessageModel> errors,
      List<ShowMessageModel> infos)
    {
      var newGroups = new List<GroupModel>();

      foreach (var group in sourceGroups)
      {
        var checkedGroup = await ProcessGroupAsync(group, context, preMeasurementDelegate, errors, infos);
        if (checkedGroup.ChainModels.Count > 0)
        {
          newGroups.Add(checkedGroup);
        }
      }

      return newGroups;
    }

    /// <summary>
    /// Проверяет все цепи внутри одной группы и собирает результирующие фрагменты.
    /// </summary>
    private static async Task<GroupModel> ProcessGroupAsync(
      GroupModel group,
      ConnectedPointContext context,
      PreMeasurementDelegate preMeasurementDelegate,
      List<ShowMessageModel> errors,
      List<ShowMessageModel> infos)
    {
      var newGroup = new GroupModel();

      foreach (var chain in group.ChainModels)
      {
        var checkedChain = await ProcessChainEntryAsync(chain, context, preMeasurementDelegate, errors, infos);
        if (checkedChain != null)
        {
          newGroup.ChainModels.AddRange(checkedChain.Fragments);
        }
      }

      return newGroup;
    }

    /// <summary>
    /// Проверяет одну исходную цепь и добавляет её сообщения в общие коллекции.
    /// </summary>
    private static async Task<ChainProcessingResult?> ProcessChainEntryAsync(
      ChainModel chain,
      ConnectedPointContext context,
      PreMeasurementDelegate preMeasurementDelegate,
      List<ShowMessageModel> errors,
      List<ShowMessageModel> infos)
    {
      var chainCopy = CloneChain(chain);
      if (!HasPoints(chainCopy))
      {
        return null;
      }

      await ShowChainCheckHeaderAsync(chainCopy, context);

      var neCommandModel = GetNeCommandModel(context);
      var isNeCommand = neCommandModel != null;
      var polarity = isNeCommand && ResolvePolarity(chain, neCommandModel);

      var result = await ProcessChainAsync(chainCopy.PointModels, context, indentLevel: 1, preMeasurementDelegate, polarity);

      LogDebug($"[ConnectedPointChecker] Chain checked. Fragments={result.Fragments.Count}. Display={BuildDisconnectionDisplayString(result.Fragments)}");

      errors.AddRange(result.Errors);
      infos.AddRange(result.Infos);

      await AppendChainErrorsAsync(result, context, errors);

      return result;
    }

    /// <summary>
    /// Проверяет, содержит ли цепь точки для обработки.
    /// </summary>
    private static bool HasPoints(ChainModel chain) => chain.PointModels.Count > 0;

    /// <summary>
    /// Показывает служебный заголовок перед началом проверки цепи.
    /// </summary>
    private static async Task ShowChainCheckHeaderAsync(ChainModel chain, ConnectedPointContext context)
    {
      var chainDisplay = BuildChainDisplayString(chain);
      LogDebug($"[ConnectedPointChecker] Start chain check. Points={chain.PointModels.Count}. Chain={chainDisplay}");

      await context.MessageService.AppendEmptyLineAsync();
      await context.MessageService.ShowMessageAsync(
        ExecutorMessageBuilder.BuildChainCheckBlock(chainDisplay),
        IsBlockStart: true);
    }

    /// <summary>
    /// Возвращает модель команды NE, если контекст действительно соответствует этой команде.
    /// </summary>
    private static NeCommandModel? GetNeCommandModel(ConnectedPointContext context)
    {
      if (context.TypeCommand != MeasurementTypeCommand.NE)
      {
        return null;
      }

      return context.CommandModel as NeCommandModel;
    }

    /// <summary>
    /// Определяет полярность подключения для цепи команды NE.
    /// </summary>
    private static bool ResolvePolarity(ChainModel chain, NeCommandModel neCommandModel)
    {
      var item = neCommandModel.ElementEnablingType.FirstOrDefault(x => x.Item1 == chain);
      return item != default && item.Item2 == ElementEnabling.Type.Direct;
    }

    /// <summary>
    /// Добавляет сообщения об ошибках по цепи в зависимости от типа измерения.
    /// </summary>
    private static async Task AppendChainErrorsAsync(
      ChainProcessingResult result,
      ConnectedPointContext context,
      List<ShowMessageModel> errors)
    {
      if (ShouldReportEveryFailedMeasurement(context))
      {
        await AppendFailedMeasurementsAsync(result, context, errors);
        return;
      }

      if (HasDisconnections(result))
      {
        await AppendDisconnectedChainErrorAsync(result, context, errors);
      }
    }

    /// <summary>
    /// Определяет, нужно ли формировать ошибку по каждому неуспешному измерению.
    /// </summary>
    private static bool ShouldReportEveryFailedMeasurement(ConnectedPointContext context) =>
      context.TypeCommand == MeasurementTypeCommand.KC || context.TypeCommand == MeasurementTypeCommand.NE;

    /// <summary>
    /// Проверяет, содержит ли результат цепи разрывы.
    /// </summary>
    private static bool HasDisconnections(ChainProcessingResult result) => result.Fragments.Count > 1;

    /// <summary>
    /// Добавляет отдельные ошибки по каждому неуспешному измерению для KC и NE.
    /// </summary>
    private static async Task AppendFailedMeasurementsAsync(
      ChainProcessingResult result,
      ConnectedPointContext context,
      List<ShowMessageModel> errors)
    {
      foreach (var failedMeasurement in result.FailedMeasurements)
      {
        var error = CreateFailedMeasurementError(context, failedMeasurement);
        errors.Add(error);

        RegisterDisconnectChainError(context, error.Header, error.Message);
        await context.MessageService.ShowMessageAsync(error);
      }
    }

    /// <summary>
    /// Создаёт сообщение об ошибке по одному неуспешному измерению.
    /// </summary>
    private static ShowMessageModel CreateFailedMeasurementError(ConnectedPointContext context, FailedMeasurement failedMeasurement)
    {
      var error = ExecutorMessageBuilder.BuildMeasurementResultMessage(
        context.TypeCommand,
        context.LowerLimit,
        context.HigherLimit,
        failedMeasurement.Value,
        chains: $"{failedMeasurement.Chain} ");

      error.Status = ShowMessageModel.MessageType.Error;
      error.IndentLevel = 2;

      return error;
    }

    /// <summary>
    /// Добавляет одну общую ошибку на всю цепь, если она распалась на несколько фрагментов.
    /// </summary>
    private static async Task AppendDisconnectedChainErrorAsync(
      ChainProcessingResult result,
      ConnectedPointContext context,
      List<ShowMessageModel> errors)
    {
      var chainStr = BuildDisconnectionDisplayString(result.Fragments);
      var error = CreateDisconnectedChainError(result, context, chainStr, out var valueForProtocol);

      errors.Add(error);
      await context.MessageService.ShowMessageAsync(error);
      RegisterDisconnectChainError(context, chainStr, valueForProtocol);
    }

    /// <summary>
    /// Создаёт общее сообщение об ошибке по цепи с разрывом.
    /// </summary>
    private static ShowMessageModel CreateDisconnectedChainError(
      ChainProcessingResult result,
      ConnectedPointContext context,
      string chainStr,
      out string valueForProtocol)
    {
      if (context.TypeCommand == MeasurementTypeCommand.PR)
      {
        valueForProtocol = string.Empty;
        return new ShowMessageModel(chainStr, type: ShowMessageModel.MessageType.Error)
        {
          IndentLevel = 2
        };
      }

      var value = result.FirstFailureValue ?? 0;
      valueForProtocol = $"{value} {ResolveUnit(context)}";

      var error = ExecutorMessageBuilder.BuildMeasurementResultMessage(
        context.TypeCommand,
        context.LowerLimit,
        context.HigherLimit,
        value,
        chainStr);

      error.Status = ShowMessageModel.MessageType.Error;
      error.IndentLevel = 2;

      return error;
    }

    /// <summary>
    /// Возвращает единицу измерения для протокола.
    /// </summary>
    private static string ResolveUnit(ConnectedPointContext context) =>
      string.IsNullOrEmpty(context.Unit) ? "Ом" : context.Unit;

    /// <summary>
    /// Регистрирует ошибку разрыва цепи в менеджере команд.
    /// </summary>
    private static void RegisterDisconnectChainError(ConnectedPointContext context, string header, string valueForProtocol)
    {
      context.CommandManager.AddErrorMethod(
        context.CommandModel.PointErrors.DisconnectChainError(
          $"{context.CommandModel.CommandNumber} {context.CommandModel.Mnemonic}",
          header,
          valueForProtocol,
          context.CommandModel.StartLineNumber,
          context.CommandModel.FormattedStartLineNumber));
    }

    /// <summary>
    /// Рекурсивная проверка цепи: первая точка подключается к нижней шине,
    /// остальные по очереди к верхней шине с тестом на связь.
    /// </summary>
    private static async Task<ChainProcessingResult> ProcessChainAsync(List<PointModel> points, ConnectedPointContext context, int indentLevel, PreMeasurementDelegate preMeasurementDelegate = null, bool revers = false)
    {
      var result = CreateInitialChainProcessingResult(points);
      if (result != null)
      {
        return result;
      }

      var state = new ChainFragmentState(points[0]);
      var messageService = context.MessageService;

      LogDebug($"[ConnectedPointChecker] Enter fragment. Count={points.Count}, Base={state.BasePoint.Mnemonic}, Indent={indentLevel}");

      await ConnectBasePointAsync(state.BasePoint, messageService, indentLevel, revers, preMeasurementDelegate);

      try
      {
        await ProcessRelativePointsAsync(points, context, indentLevel, revers, state);
      }
      finally
      {
        await DisconnectBasePointAsync(state.BasePoint, messageService, revers);
      }

      return await CompleteFragmentProcessingAsync(context, indentLevel, preMeasurementDelegate, state);
    }

    /// <summary>
    /// Создаёт базовый результат для пустой или одноточечной цепи.
    /// </summary>
    private static ChainProcessingResult? CreateInitialChainProcessingResult(List<PointModel> points)
    {
      var result = new ChainProcessingResult();

      if (points == null || points.Count == 0)
      {
        return result;
      }

      if (points.Count == 1)
      {
        result.Fragments.Add(new ChainModel(new List<PointModel>(points)));
        return result;
      }

      return null;
    }

    /// <summary>
    /// Подключает базовую точку фрагмента к нижней шине и выполняет подготовку перед измерением.
    /// </summary>
    private static async Task ConnectBasePointAsync(
      PointModel basePoint,
      IUserInteractionService messageService,
      int indentLevel,
      bool revers,
      PreMeasurementDelegate preMeasurementDelegate)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel($"Подлючение точек") { IndentLevel = indentLevel }, IsBlockStart: true);
      await DeviceManager.RelayModule.PointManager.ConnectPointToBusBAsync(basePoint, messageService, revers);

      if (preMeasurementDelegate != null)
      {
        await preMeasurementDelegate(messageService.GetCancellationToken());
      }
    }

    /// <summary>
    /// Отключает базовую точку фрагмента от нижней шины.
    /// </summary>
    private static Task DisconnectBasePointAsync(PointModel basePoint, IUserInteractionService messageService, bool revers) =>
      DeviceManager.RelayModule.PointManager.DisconnectPointFromBusBAsync(basePoint, messageService, revers);

    /// <summary>
    /// Последовательно проверяет все точки фрагмента относительно базовой точки.
    /// </summary>
    private static async Task ProcessRelativePointsAsync(
      List<PointModel> points,
      ConnectedPointContext context,
      int indentLevel,
      bool revers,
      ChainFragmentState state)
    {
      foreach (var point in points.Skip(1))
      {
        context.MessageService.GetCancellationToken().ThrowIfCancellationRequested();
        await ProcessRelativePointAsync(point, context, indentLevel, revers, state);
      }
    }

    /// <summary>
    /// Проверяет одну точку относительно базовой и обновляет состояние текущего фрагмента.
    /// </summary>
    private static async Task ProcessRelativePointAsync(
      PointModel point,
      ConnectedPointContext context,
      int indentLevel,
      bool revers,
      ChainFragmentState state)
    {
      var messageService = context.MessageService;

      await ShowPointCheckHeaderAsync(state.BasePoint, point, messageService);
      await DeviceManager.RelayModule.PointManager.ConnectPointToBusAAsync(point, messageService, revers);

      try
      {
        var measured = await MeasurePointAsync(point, context, messageService);
        var chainStr = BuildChainString(context, state.BasePoint, point);

        LogMeasurement(state.BasePoint, point, measured);
        UpdateFragmentState(state, point, measured, context, chainStr);
        AddProtocolInfo(state, context, indentLevel, measured, chainStr);
      }
      finally
      {
        await DeviceManager.RelayModule.PointManager.DisconnectPointFromBusAAsync(point, messageService, revers);
      }
    }

    /// <summary>
    /// Показывает заголовок проверки пары точек.
    /// </summary>
    private static Task ShowPointCheckHeaderAsync(PointModel basePoint, PointModel point, IUserInteractionService messageService) =>
      messageService.ShowMessageAsync(
        ExecutorMessageBuilder.BuildPointsCheckHeaderAsync(basePoint, point, CircuitFaultType.ShortCircuit),
        IsBlockStart: true);

    /// <summary>
    /// Выполняет измерение для точки с учётом типа команды и параметров модуля.
    /// </summary>
    private static async Task<(bool Result, double Value)> MeasurePointAsync(
      PointModel point,
      ConnectedPointContext context,
      IUserInteractionService messageService)
    {
      var module = EquipmentService.GetModuleByPoint(point);
      var errorResistance = GetMeasurementErrorValue(context, module);

      return await context.PerformMeasurementAsync(
        context.Value,
        messageService,
        messageService.GetCancellationToken(),
        errorResistance);
    }

    /// <summary>
    /// Возвращает параметр модуля, который должен использоваться как допуск измерения.
    /// </summary>
    private static double GetMeasurementErrorValue(ConnectedPointContext context, global::Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.IRelaySwitchModule module)
    {
      if (context.TypeCommand == MeasurementTypeCommand.IE)
      {
        return module.SwitchCapacitance;
      }

      return module.SwitchResistance;
    }

    /// <summary>
    /// Строит строковое представление пары точек для сообщений и протокола.
    /// </summary>
    private static string BuildChainString(ConnectedPointContext context, PointModel basePoint, PointModel point)
    {
      var chain = new ChainModel(new List<PointModel> { basePoint, point });
      return context.CommandModel.BuildDislpayInfo.BuildErrorChainStringAsync(chain);
    }

    /// <summary>
    /// Логирует результат измерения между двумя точками.
    /// </summary>
    private static void LogMeasurement(PointModel basePoint, PointModel point, (bool Result, double Value) measured)
    {
      LogDebug($"[ConnectedPointChecker] Test {basePoint.Mnemonic}->{point.Mnemonic}. Result={(measured.Result ? "OK" : "FAIL")} Value={measured.Value}");
    }

    /// <summary>
    /// Обновляет состояние текущего фрагмента по результату измерения точки.
    /// </summary>
    private static void UpdateFragmentState(
      ChainFragmentState state,
      PointModel point,
      (bool Result, double Value) measured,
      ConnectedPointContext context,
      string chainStr)
    {
      if (!measured.Result)
      {
        state.BrokenPoints.Add(point);
        state.FirstFailureValue ??= measured.Value;

        if (ShouldReportEveryFailedMeasurement(context))
        {
          state.Result.FailedMeasurements.Add(new FailedMeasurement(chainStr, measured.Value));
        }

        return;
      }

      state.ConnectedPoints.Add(point);
    }

    /// <summary>
    /// Добавляет информационное сообщение в протокол, если это предусмотрено контекстом.
    /// </summary>
    private static void AddProtocolInfo(
      ChainFragmentState state,
      ConnectedPointContext context,
      int indentLevel,
      (bool Result, double Value) measured,
      string chainStr)
    {
      if (!context.IsProtocolAttribute)
      {
        return;
      }

      var info = ExecutorMessageBuilder.BuildMeasurementResultMessage(
        context.TypeCommand,
        context.LowerLimit,
        context.HigherLimit,
        measured.Value,
        chainStr);

      info.IndentLevel = indentLevel + 1;
      state.Result.Infos.Add(info);
    }

    /// <summary>
    /// Формирует итоговый результат по текущему фрагменту и при необходимости запускает рекурсивную проверку разрывов.
    /// </summary>
    private static async Task<ChainProcessingResult> CompleteFragmentProcessingAsync(
      ConnectedPointContext context,
      int indentLevel,
      PreMeasurementDelegate preMeasurementDelegate,
      ChainFragmentState state)
    {
      var connectedFragment = CreateConnectedFragment(state);
      state.Result.Fragments.Add(connectedFragment);
      state.Result.FirstFailureValue ??= state.FirstFailureValue;

      if (ShouldProcessBrokenPointsRecursively(state, context))
      {
        var nextFragment = await ProcessChainAsync(state.BrokenPoints, context, indentLevel + 1, preMeasurementDelegate);
        state.Result.Append(nextFragment);
      }
      else
      {
        LogDebug($"[ConnectedPointChecker] Fragment is intact. Count={connectedFragment.PointModels.Count}");
      }

      return state.Result;
    }

    /// <summary>
    /// Создаёт связный фрагмент из базовой точки и всех успешно проверенных точек.
    /// </summary>
    private static ChainModel CreateConnectedFragment(ChainFragmentState state) =>
      new ChainModel(new List<PointModel>(state.ConnectedPoints));

    /// <summary>
    /// Определяет, нужно ли рекурсивно проверять точки, на которых обнаружен разрыв.
    /// </summary>
    private static bool ShouldProcessBrokenPointsRecursively(ChainFragmentState state, ConnectedPointContext context) =>
      state.BrokenPoints.Count > 0 && !ShouldReportEveryFailedMeasurement(context);

    /// <summary>
    /// Строит отображение цепи в пользовательских сообщениях.
    /// </summary>
    private static string BuildChainDisplayString(ChainModel chain)
    {
      var builder = new StringBuilder("*");

      for (int i = 0; i < chain.PointModels.Count; i++)
      {
        var point = chain.PointModels[i];
        var address = DeviceDisplayConfig.GetMachineAddressVisibility() ? $"[{point.ToString()}]" : string.Empty;
        var delimiter = (i + 1) == chain.PointModels.Count ? "*" : ",";
        builder.Append($"{point.Mnemonic}{address}{delimiter}");
      }

      return builder.ToString();
    }

    /// <summary>
    /// Создаёт поверхностную копию цепи для безопасной обработки.
    /// </summary>
    private static ChainModel CloneChain(ChainModel source)
    {
      var clone = new ChainModel();
      clone.PointModels.AddRange(source.PointModels);
      return clone;
    }

    /// <summary>
    /// Строит строку отображения цепи с учётом найденных разрывов и фрагментов.
    /// </summary>
    private static string BuildDisconnectionDisplayString(List<ChainModel> fragments)
    {
      var fragmentStrings = fragments.Select(fragment =>
      {
        var points = fragment.PointModels.Select(p =>
        {
          var address = DeviceDisplayConfig.GetMachineAddressVisibility() ? $" [{p.ToString()}]" : string.Empty;
          return $"{p.Mnemonic}{address}";
        });

        return string.Join(",", points);
      });

      return $"*{string.Join("**", fragmentStrings)}*";
    }

    /// <summary>
    /// Состояние рекурсивной проверки одного фрагмента цепи.
    /// </summary>
    private sealed class ChainFragmentState
    {
      public ChainFragmentState(PointModel basePoint)
      {
        BasePoint = basePoint;
        ConnectedPoints.Add(basePoint);
      }

      public PointModel BasePoint { get; }
      public List<PointModel> ConnectedPoints { get; } = new();
      public List<PointModel> BrokenPoints { get; } = new();
      public ChainProcessingResult Result { get; } = new();
      public double? FirstFailureValue { get; set; }
    }

    /// <summary>
    /// Модель неуспешного измерения между двумя точками.
    /// </summary>
    private sealed class FailedMeasurement
    {
      public FailedMeasurement(string chain, double value)
      {
        Chain = chain;
        Value = value;
      }

      public string Chain { get; }
      public double Value { get; }
    }

    /// <summary>
    /// Агрегированный результат проверки цепи и всех её дочерних фрагментов.
    /// </summary>
    private sealed class ChainProcessingResult
    {
      public List<ChainModel> Fragments { get; } = new();
      public List<ShowMessageModel> Errors { get; } = new();
      public List<ShowMessageModel> Infos { get; } = new();
      public List<FailedMeasurement> FailedMeasurements { get; } = new();
      public double? FirstFailureValue { get; set; }

      /// <summary>
      /// Добавляет в текущий результат данные дочернего фрагмента.
      /// </summary>
      public void Append(ChainProcessingResult other)
      {
        if (other == null)
          return;

        Fragments.AddRange(other.Fragments);
        Errors.AddRange(other.Errors);
        Infos.AddRange(other.Infos);
        FailedMeasurements.AddRange(other.FailedMeasurements);
        FirstFailureValue ??= other.FirstFailureValue;
      }
    }
  }
}
