using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.Tests.MethodExecutor.MeasurementSystem
{
  /// <summary>
  /// Отвечает за пошаговое выполнение теста для всех модулей в параллельном режиме.
  /// </summary>
  public class ParallelTestRunner
  {
    /// <summary>
    /// Шина, которая считается активной (используется при значении бита '1').
    /// </summary>
    private readonly BusPoint _assignedBus;

    /// <summary>
    /// Шина, противоположная активной (используется при значении бита '0').
    /// </summary>
    private readonly BusPoint _oppositeBus;

    /// <summary>
    /// Общее количество разрядов в двоичном представлении номера точки.
    /// </summary>
    private readonly int _bitLength;

    /// <summary>
    /// Интерфейс для вывода сообщений в протокол выполнения.
    /// </summary>
    private readonly IUserInteractionService _protocolUI;

    /// <summary>
    /// Делегат, выполняющий измерение на каждом шаге теста.
    /// </summary>
    private readonly Func<IUserInteractionService, DataModel, Task> _measurementAction;

    /// <summary>
    /// Шаг выполнения.
    /// </summary>
    public int Step { get; private set; }

    public int CurrentDischargeNumber => Step + 1;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ParallelTestRunner"/>.
    /// </summary>
    /// <param name="assignedBus">Шина, которая считается активной (для бита '1').</param>
    /// <param name="oppositeBus">Противоположная шина (для бита '0').</param>
    /// <param name="bitLength">Общее количество разрядов в двоичном представлении точек.</param>
    /// <param name="protocolUI">Интерфейс для вывода сообщений в протокол.</param>
    /// <param name="measurementAction">Делегат, выполняющий измерение на текущем шаге.</param>
    public ParallelTestRunner(
        BusPoint assignedBus,
        BusPoint oppositeBus,
        int bitLength,
        IUserInteractionService protocolUI,
        Func<IUserInteractionService, DataModel, Task> measurementAction)
    {
      _assignedBus = assignedBus;
      _oppositeBus = oppositeBus;
      _bitLength = bitLength;
      _protocolUI = protocolUI;
      _measurementAction = measurementAction;
    }

    /// <summary>
    /// Запускает выполнение тестов по шагам для всех групп точек.
    /// </summary>
    /// <param name="dataModel">Модель данных, содержащая диапазон точек и параметры теста.</param>
    /// <param name="groupedPoints">Список сгруппированных точек с бинарными строками по модулям.</param>
    /// <param name="groupingService">Сервис группировки точек и назначения шин подключения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task RunAsync(
      DataModel dataModel,
      List<(IRelaySwitchModule module, List<PointModel> points, List<string> reversedBinary)> groupedPoints,
      PointGroupingService groupingService,
      CancellationToken cancellationToken)
    {
      for (int step = 0; step < _bitLength; step++)
      {
        Step = step;

        await ShowCurrentBitStep(step);

        var tasks = groupedPoints.Select(group =>
            ConnectPointsToBusAsync(group, groupingService, step, cancellationToken));

        await Task.WhenAll(tasks);

        await _measurementAction(_protocolUI, dataModel);
        await _protocolUI.ShowMessageAsync(new ShowMessageModel($"\tОбщий сброс точек"));

        await ResetAllPointsAsync(groupedPoints, cancellationToken);
      }
    }

    /// <summary>
    /// Показывает информацию о текущем проверяемом разряде.
    /// </summary>
    /// <param name="step">Индекс текущего разряда.</param>
    private async Task ShowCurrentBitStep(int step)
    {
      string bitString = GetBitString();
      await _protocolUI.AppendEmptyLineAsync();
      await _protocolUI.ShowMessageAsync(new ShowMessageModel($"Проверка разряда {step + 1} ({bitString})"));
    }

    /// <summary>
    /// Подключает все точки группы к соответствующей шине в зависимости от текущего разряда.
    /// </summary>
    private async Task ConnectPointsToBusAsync(
      (IRelaySwitchModule module, List<PointModel> points, List<string> reversedBinary) group,
      PointGroupingService groupingService,
      int step,
      CancellationToken cancellationToken)
    {
      var busAssignments = groupingService
        .AssignBusConnections(new List<(IRelaySwitchModule, List<PointModel>, List<string>)> { group }, step, _assignedBus, _oppositeBus)
        .First().buses;

      var (module, points, _) = group;

      for (int i = 0; i < points.Count; i++)
      {
        cancellationToken.ThrowIfCancellationRequested();
        var point = points[i];
        var bus = busAssignments[i];

        await module.PointManager.ConnectRelayAsync(bus, point.PointNumber, _protocolUI);
      }

      await Task.Delay(500);
    }

    /// <summary>
    /// Выполняет отключение всех точек после завершения шага.
    /// </summary>
    /// <param name="groups">Группы точек по модулям.</param>
    private async Task ResetAllPointsAsync(
      List<(IRelaySwitchModule module, List<PointModel> points, List<string> reversedBinary)> groups,
      CancellationToken cancellationToken)
    {
      foreach (var (module, points, _) in groups)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await module.ConnectableManager.ResetAsync(_protocolUI);
        await module.BusManager.ConnectBusAsync(SwitchingBus.A1, userMessageService: _protocolUI);
        await module.BusManager.ConnectBusAsync(SwitchingBus.B1, userMessageService: _protocolUI);
      }
    }

    /// <summary>
    /// Возвращает строку, в которой только текущий бит равен '1', а остальные — '0'.
    /// </summary>
    /// <param name="step">Текущий шаг (разряд), начиная с младшего.</param>
    /// <returns>Двоичная строка, где установлен только один бит.</returns>
    public string GetBitString()
    {
      var chars = Enumerable.Repeat('0', _bitLength).ToArray();
      chars[_bitLength - 1 - Step] = '1';
      return new string(chars);
    }
  }
}
