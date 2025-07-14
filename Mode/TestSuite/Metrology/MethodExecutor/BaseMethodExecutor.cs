using Mode.Base;
using Mode.Models;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities.Interface;
using static NewCore.Enum.DeviceEnum;

namespace Mode.TestSuite.Metrology.MethodExecutor
{
  /// <summary>
  /// Базовый класс для тестирования группы точек.
  /// </summary>
  public abstract class BaseMethodExecutor
  {
    private readonly DeviceCollector _deviceCollector = new();
    private CommutationManager _commutationManager;
    private BinaryPointMapper _binaryPointMapper;
    private PointGroupingService _pointGroupingService;
    private ParallelTestRunner _runner;

    /// <summary>
    /// Активная шина, соответствующая биту '1'.
    /// </summary>
    protected BusPoint AssignedBus { get; private set; }

    /// <summary>
    /// Противоположная шина, соответствующая биту '0'.
    /// </summary>
    protected BusPoint OppositeBus { get; private set; }

    /// <summary>
    /// Список задействованных в тесте устройств.
    /// </summary>
    protected List<object> Devices => _deviceCollector.Devices;

    /// <summary>
    /// Количество разрядов в двоичном представлении номера точки.
    /// </summary>
    protected int HighestBitCount { get; private set; }

    /// <summary>
    /// Настраивает измерительное устройство (мультиметр или ППУ).
    /// </summary>
    /// <param name="dataModel">Модель данных, содержащая параметры измерений.</param>
    public abstract Task ConfigureMeter(IUserMessageService messageService, DataModel dataModel = null);

    /// <summary>
    /// Выполняет измерение.
    /// </summary>
    /// <param name="protocolUI">Интерфейс протокола для вывода сообщений.</param>
    /// <param name="dataModel">Модель данных, содержащая параметры измерений.</param>
    public abstract Task PerformMeasurement(ProtocolUI protocolUI, DataModel dataModel);

    /// <summary>
    /// Подключает все устройства и инициализирует компоненты.
    /// </summary>
    /// <param name="point1">Начальная точка диапазона.</param>
    /// <param name="point2">Конечная точка диапазона.</param>
    /// <param name="protocolUI">Интерфейс протокола для вывода сообщений.</param>
    /// <returns>Результат подключения.</returns>
    public virtual async Task<(bool Connect, string Message)> ConnectToEquipment(PointModel point1, PointModel point2, ProtocolUI protocolUI)
    {
      try
      {
        _deviceCollector.Collect(point1, point2);
        _commutationManager = new CommutationManager(_deviceCollector.Devices);
        _binaryPointMapper = new BinaryPointMapper(_deviceCollector.Devices.OfType<IRelaySwitchModule>());
        _pointGroupingService = new PointGroupingService(_deviceCollector.Devices.OfType<IRelaySwitchModule>());
      }
      catch (Exception ex)
      {
        return (false, ex.Message);
      }

      return await _deviceCollector.ConnectAllAsync(protocolUI);
    }

    /// <summary>
    /// Настраивает коммутацию перед измерением.
    /// </summary>
    /// <param name="protocolUI">Интерфейс протокола для вывода сообщений.</param>
    /// <param name="point1">Начальная точка диапазона.</param>
    /// <param name="point2">Конечная точка диапазона.</param>
    /// <param name="bus">Активная шина подключения.</param>
    public virtual async Task SetupCommutation(ProtocolUI protocolUI, PointModel point1, PointModel point2, BusPoint bus)
    {
      AssignedBus = bus;
      OppositeBus = bus == BusPoint.A ? BusPoint.B : BusPoint.A;

      if (_commutationManager == null)
      {
        throw new InvalidOperationException("CommutationManager не инициализирован. Сначала вызовите ConnectToEquipment.");
      }

      await _commutationManager.SetupAsync(protocolUI, bus);
    }

    /// <summary>
    /// Запускает пошаговый алгоритм измерения.
    /// </summary>
    /// <param name="protocolUI">Интерфейс протокола для вывода сообщений.</param>
    /// <param name="dataModel">Модель данных, содержащая параметры измерений.</param>
    public async Task RunParallelModuleTasksAsync(ProtocolUI protocolUI, DataModel dataModel)
    {
      if (_binaryPointMapper == null || _pointGroupingService == null)
      {
        throw new InvalidOperationException("Компоненты не инициализированы. Сначала вызовите ConnectToEquipment.");
      }

      AssignedBus = dataModel.ActiveBus;
      OppositeBus = AssignedBus == BusPoint.A ? BusPoint.B : BusPoint.A;

      HighestBitCount = _binaryPointMapper.GetHighestPointBinaryDigits(dataModel.FirstPoint, dataModel.SecondPoint);
      var binaryPoints = _binaryPointMapper.ConvertToReversedBinaryRange(dataModel.FirstPoint, dataModel.SecondPoint, HighestBitCount);
      var grouped = _pointGroupingService.GroupByModulesWithBinary(binaryPoints);

      _runner = new ParallelTestRunner(
          AssignedBus,
          OppositeBus,
          HighestBitCount,
          protocolUI,
          async (ui, model) => await PerformMeasurement(ui, model));

      await _runner.RunAsync(dataModel, grouped, _pointGroupingService, protocolUI.GetCancellationToken());
    }

    /// <summary>
    /// Выполняет общий сброс оборудования.
    /// </summary>
    public virtual async Task FinalizeAsync(IUserMessageService messageService)
    {
      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
    }

    /// <summary>
    /// Возвращает строку, где заданный разряд равен 1, остальные — 0.
    /// </summary>
    protected string GetBitString() => _runner.GetBitString();
  }
}
