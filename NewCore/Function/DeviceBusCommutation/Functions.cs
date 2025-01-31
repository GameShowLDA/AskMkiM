using NewCore.Device;

namespace NewCore.Function.DeviceBusCommutation
{
  /// <summary>
  /// Класс <see cref="Functions"/> предоставляет доступ к различным менеджерам для управления устройствами и функциями УКШ (Устройства Коммутации Шин).
  /// </summary>
  /// <remarks>
  /// Этот класс служит точкой входа для работы с различными типами устройств и их функциональностью через единый интерфейс.
  /// </remarks>
  public class Functions
  {
    /// <summary>
    /// Экземпляр устройства коммутации шин, используемый для отправки команд.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Functions"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public Functions(Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation;
    }

    #region Менеджеры команд.

    /// <summary>
    /// Возвращает менеджер для управления шинами УКШ.
    /// </summary>
    /// <remarks>
    /// Позволяет выполнять операции по подключению/отключению шин, управлению цепочками и самоконтролю.
    /// </remarks>
    public BusManager BusManager => new BusManager(_deviceBusCommutation);

    /// <summary>
    /// Возвращает менеджер для управления резисторами УКШ.
    /// </summary>
    /// <remarks>
    /// Предоставляет функциональность для настройки и управления резисторами в системе.
    /// </remarks>
    public ResistorManager ResistorManager => new ResistorManager(_deviceBusCommutation);

    /// <summary>
    /// Возвращает менеджер для управления конденсаторами УКШ.
    /// </summary>
    /// <remarks>
    /// Позволяет выполнять операции по настройке и управлению конденсаторами в системе.
    /// </remarks>
    public CapacitorManager CapacitorManager => new CapacitorManager(_deviceBusCommutation);

    /// <summary>
    /// Возвращает менеджер для управления реле УКШ.
    /// </summary>
    /// <remarks>
    /// Предоставляет функциональность для управления состоянием реле (включение/выключение).
    /// </remarks>
    public RelayManager RelayManager => new RelayManager(_deviceBusCommutation);

    /// <summary>
    /// Возвращает менеджер для управления разъемами УКШ.
    /// </summary>
    /// <remarks>
    /// Позволяет выполнять операции по подключению/отключению разъемов и управлению их состоянием.
    /// </remarks>
    public ConnectorManager ConnectorManager => new ConnectorManager(_deviceBusCommutation);

    /// <summary>
    /// Возвращает менеджер для получения состояния УКШ.
    /// </summary>
    /// <remarks>
    /// Предоставляет функциональность для запроса текущего состояния устройств и их параметров.
    /// </remarks>
    public StateManager StateManager => new StateManager(_deviceBusCommutation);

    #endregion
  }
}