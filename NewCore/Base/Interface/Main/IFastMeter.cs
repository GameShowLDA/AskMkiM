using NewCore.Base.Function.FastMeter;
using NewCore.Base.Interface.Additionally;

namespace NewCore.Base.Interface.Main
{
  /// <summary>
  /// Интерфейс для быстрого измерителя.
  /// </summary>
  public interface IFastMeter : IAttachableDevice
  {
    /// <summary>
    /// Управление измерением переменного напряжения.
    /// </summary>
    public IAcVoltageMeasurement AcVoltageManager { get; set; }

    /// <summary>
    /// Управление измерением ёмкости.
    /// </summary>
    public ICapacitanceMeasurement CapacitanceManager { get; set; }

    /// <summary>
    /// Управление коммуникацией с устройством.
    /// </summary>
    public ICommunication CommunicationManager { get; set; }

    /// <summary>
    /// Управление соединением с устройством.
    /// </summary>
    public IConnection ConnectionManager { get; set; }

    /// <summary>
    /// Управление измерением проводимости (прозвонка).
    /// </summary>
    public IContinuityMeasurement ContinuityManager { get; set; }

    /// <summary>
    /// Управление измерением постоянного напряжения.
    /// </summary>
    public IDcVoltageMeasurement DcVoltageManager { get; set; }

    /// <summary>
    /// Управление измерением сопротивления.
    /// </summary>
    public IResistanceMeasurement ResistanceManager { get; set; }
  }
}
