using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
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
    internal delegate Task<(bool Result, double Value)> PerformMeasurementAsync(double value, IUserInteractionService userMessageService, CancellationToken cancellationToken, PointModel point, double errorResistance);

    /// <summary>
    /// Асинхронно выполняет проверку соединённых точек в схеме, формируя новый список цепей (ССИРТ)
    /// с учётом обнаруженных разрывов.
    /// </summary>
    static public async Task<AlgorithmExecutionResult> CheckSequenceAsync(ConnectedPointContext context, PreMeasurementDelegate preMeasurementDelegate = null)
    {
      var messages = new AlgorithmExecutionResult(new(), new());
      var sourceGroups = GetSourceGroups(context);

      if (!HasGroupsToProcess(sourceGroups))
      {
        return messages;
      }
      if (context.TypeCommand != MeasurementTypeCommand.NE)
      {
        await PublishCheckBlockHeaderAsync(context);
      }

      var newGroups = await BuildCheckedGroupsAsync(sourceGroups, context, preMeasurementDelegate, messages);
      context.NewScheme = new SchemeModel(newGroups);

      return messages;
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
    private static Task PublishCheckBlockHeaderAsync(ConnectedPointContext context)
    {
      var algorithm = context.TypeCommand == MeasurementTypeCommand.KC
        ? ControlCheckAlgorithm.ResistanceRelativeToFirstPoint
        : ControlCheckAlgorithm.MessageRelativeToFirstPoint;

      return CommandMessages.PublishCheckBlockHeaderAsync(
        context.MessageService,
        algorithm,
        context.IsPolarityReversed);
    }

    /// <summary>
    /// Формирует новый набор групп после проверки всех цепей исходной схемы.
    /// </summary>
    private static async Task<List<GroupModel>> BuildCheckedGroupsAsync(
      List<GroupModel> sourceGroups,
      ConnectedPointContext context,
      PreMeasurementDelegate preMeasurementDelegate,
      AlgorithmExecutionResult messages)
    {
      var newGroups = new List<GroupModel>();

      foreach (var group in sourceGroups)
      {
        var checkedGroup = await ProcessGroupAsync(group, context, preMeasurementDelegate, messages);
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
      AlgorithmExecutionResult messages)
    {
      var newGroup = new GroupModel();

      foreach (var chain in group.ChainModels)
      {
        var checkedChain = await ProcessChainEntryAsync(chain, context, preMeasurementDelegate, messages);
        if (checkedChain != null)
        {
          newGroup.ChainModels.AddRange(checkedChain.Fragments);
        }
      }

      return newGroup;
    }

    /// <summary>
    /// Проверяет одну исходную цепь и для команды NE выполняет
    /// два прогона: в прямом и обратном направлении.
    /// </summary>
    private static async Task<ChainProcessingResult?> ProcessChainEntryAsync(
      ChainModel chain,
      ConnectedPointContext context,
      PreMeasurementDelegate preMeasurementDelegate,
      AlgorithmExecutionResult messages)
    {
      var chainCopy = CloneChain(chain);
      if (!HasPoints(chainCopy))
      {
        return null;
      }

      await context.MessageService.WaitIfPausedAsync();

      if (context.TypeCommand != MeasurementTypeCommand.NE)
      {
        await ShowChainCheckHeaderAsync(chainCopy, context);
      }

      var neCommandModel = GetNeCommandModel(context);
      var isNeCommand = neCommandModel != null;
      var directPolarity = isNeCommand && ResolvePolarity(chain, neCommandModel!); // направление проверки диода

      var result = await RunChainPassAsync(
        chainCopy.PointModels,
        context,
        preMeasurementDelegate,
        directPolarity,
        messages,
        isNeCommand,
        isDirectDirection: true,
        currentNeDirectionSign: ResolveNeDirectionSign(directPolarity, isDirectPass: true));

      if (isNeCommand)
      {
        await RunChainPassAsync(
          chainCopy.PointModels,
          context,
          preMeasurementDelegate,
          !directPolarity,
          messages,
          isNeCommand: true,
          isDirectDirection: false,
          currentNeDirectionSign: ResolveNeDirectionSign(directPolarity, isDirectPass: false));
      }

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

      if (ProtocolConfig.GetTestStepMessagesInProtocol())
      {
        await context.MessageService.AppendEmptyLineAsync();
      }

      await CommandMessages.PublishChainCheckBlockAsync(context.MessageService, chainDisplay);
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
    /// Выполняет один прогон проверки цепи и при необходимости показывает направление проверки для NE.
    /// </summary>
    private static async Task<ChainProcessingResult> RunChainPassAsync(
      List<PointModel> points,
      ConnectedPointContext context,
      PreMeasurementDelegate preMeasurementDelegate,
      bool polarity,
      AlgorithmExecutionResult messages,
      bool isNeCommand,
      bool isDirectDirection,
      string? currentNeDirectionSign)
    {
      await ShowNeDirectionMessageIfRequiredAsync(context, isNeCommand, isDirectDirection);

      var previousOverloadExpectation = context.IsOverloadExpected;
      var previousDirectionSign = context.CurrentNeDirectionSign;
      context.IsOverloadExpected = isNeCommand && !isDirectDirection;
      context.CurrentNeDirectionSign = currentNeDirectionSign;

      ChainProcessingResult result;

      try
      {
        result = await ProcessChainAsync(points, context, indentLevel: 1, preMeasurementDelegate, polarity);
      }
      finally
      {
        context.IsOverloadExpected = previousOverloadExpectation;
        context.CurrentNeDirectionSign = previousDirectionSign;
      }

      LogDebug($"[ConnectedPointChecker] Chain checked. Fragments={result.Fragments.Count}. Display={BuildDisconnectionDisplayString(result.Fragments)}");

      await context.MessageService.WaitIfPausedAsync();

      messages.AddRange(result.Messages);

      await AppendChainErrorsAsync(result, context, messages);

      return result;
    }

    /// <summary>
    /// Возвращает знак направления для протокола NE с учётом текущего прохода.
    /// </summary>
    private static string ResolveNeDirectionSign(bool isOriginalDirect, bool isDirectPass)
    {
      var isDirectSign = isDirectPass ? isOriginalDirect : !isOriginalDirect;
      return isDirectSign
        ? ElementEnabling.Type.Direct.GetDescription()
        : ElementEnabling.Type.Reverse.GetDescription();
    }

    /// <summary>
    /// Показывает сообщение о направлении проверки диода для команды NE.
    /// </summary>
    private static Task ShowNeDirectionMessageIfRequiredAsync(
      ConnectedPointContext context,
      bool isNeCommand,
      bool isDirectDirection)
    {
      if (!isNeCommand)
      {
        return Task.CompletedTask;
      }

      return CommandMessages.PublishDiodeDirectionAsync(context.MessageService, isDirectDirection);
    }

    /// <summary>
    /// Добавляет сообщения об ошибках по цепи в зависимости от типа измерения.
    /// </summary>
    private static async Task AppendChainErrorsAsync(
      ChainProcessingResult result,
      ConnectedPointContext context,
      AlgorithmExecutionResult messages)
    {
      if (ShouldReportEveryFailedMeasurement(context))
      {
        await AppendFailedMeasurementsAsync(result, context, messages);
        return;
      }

      if (HasDisconnections(result))
      {
        await AppendDisconnectedChainErrorAsync(result, context, messages);
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
      AlgorithmExecutionResult messages)
    {
      foreach (var failedMeasurement in result.FailedMeasurements)
      {
        var range = new MeasurementRange(
          failedMeasurement.Value,
          context.LowerLimit,
          context.HigherLimit);
        var error = MeasurementMessages.BuildMeasurementResultMessage(
          context.TypeCommand,
          range,
          false,
          failedMeasurement.Chain,
          indentLevel: 2);
        messages.Errors.Add(error);

        RegisterDisconnectChainError(context, error.Header, error.Message);
        await MeasurementMessages.PublishResultAsync(CheckType.ControlProgram,
          context.TypeCommand,
          range,
          false,
          failedMeasurement.Chain,
          outputService: context.MessageService);
      }
    }

    /// <summary>
    /// Добавляет одну общую ошибку на всю цепь, если она распалась на несколько фрагментов.
    /// </summary>
    private static async Task AppendDisconnectedChainErrorAsync(
      ChainProcessingResult result,
      ConnectedPointContext context,
      AlgorithmExecutionResult messages)
    {
      var chainStr = BuildDisconnectionDisplayString(result.Fragments);
      var value = result.FirstFailureValue ?? 0;
      var range = new MeasurementRange(value, context.LowerLimit, context.HigherLimit);
      var error = MeasurementMessages.BuildMeasurementResultMessage(
        context.TypeCommand,
        range,
        false,
        chainStr,
        indentLevel: 2);
      var valueForProtocol = MeasurementValueFormatter.FormatWithUnit(value, ResolveUnit(context));

      messages.Errors.Add(error);
      await MeasurementMessages.PublishResultAsync(CheckType.ControlProgram,
        context.TypeCommand,
        range,
        false,
        chainStr,
        outputService: context.MessageService);
      RegisterDisconnectChainError(context, chainStr, valueForProtocol);
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
      await CommandMessages.PublishPointsConnectionAsync(messageService, indentLevel);
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
      if (context.TypeCommand != MeasurementTypeCommand.NE)
      {
        await ShowPointCheckHeaderAsync(state.BasePoint, point, messageService);
      }
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
    private static async Task ShowPointCheckHeaderAsync(PointModel basePoint, PointModel point, IUserInteractionService messageService)
    {
      await CommandMessages.PublishPointsCheckHeaderAsync(
        messageService,
        basePoint,
        point,
        CircuitFaultType.ShortCircuit);
    }

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
        point,
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

      if (context.TypeCommand == MeasurementTypeCommand.NE)
      {
        return BuildNeProtocolChainString(chain, context.CurrentNeDirectionSign);
      }

      return context.CommandModel.BuildDislpayInfo.BuildErrorChainStringAsync(chain);
    }

    /// <summary>
    /// Формирует строку цепи для протокола NE со знаком направления перед первой точкой.
    /// </summary>
    private static string BuildNeProtocolChainString(ChainModel chain, string? directionSign)
    {
      if (chain.PointModels.Count == 0)
      {
        return string.Empty;
      }

      var parts = new List<string>(chain.PointModels.Count);

      for (int index = 0; index < chain.PointModels.Count; index++)
      {
        var point = chain.PointModels[index];

        string machineAddress = string.Empty;

        if (DeviceDisplayConfig.GetMachineAddressVisibility())
        {
          if (ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
          {
            machineAddress = $"[{LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(point.ToString())}]";
          }
          else
          {
            machineAddress = $"[{point.ToString()}]";
          }
        }

        var pointText = $"{point.Mnemonic}{machineAddress}";
        if (index == 0 && !string.IsNullOrEmpty(directionSign))
        {
          pointText = $"{directionSign}{pointText}";
        }

        parts.Add(pointText);
      }

      return string.Join(", ", parts);
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

      var info = MeasurementMessages.BuildMeasurementResultMessage(
        context.TypeCommand,
        new MeasurementRange(measured.Value, context.LowerLimit, context.HigherLimit),
        measured.Result,
        chainStr,
        indentLevel + 1);

      state.Result.Messages.Info.Add(info);
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
        string address = string.Empty;
        if (DeviceDisplayConfig.GetMachineAddressVisibility())
        {
          if (!ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
          {
            address = $"[{point.ToString()}]";
          }
          else
          {
            address = $"[{LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(point.ToString())}]";
          }
        }

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
          string address = string.Empty;

          if (DeviceDisplayConfig.GetMachineAddressVisibility())
          {
            if (ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
            {
              address = $"[{LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(p.ToString())}]";
            }
            else
            {
              address = $"[{p.ToString()}]";
            }
          }

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
      public AlgorithmExecutionResult Messages { get; } = new(new(), new());
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
        Messages.AddRange(other.Messages);
        FailedMeasurements.AddRange(other.FailedMeasurements);
        FirstFailureValue ??= other.FirstFailureValue;
      }
    }
  }
}
