using NewCore.Base.Function.FastMeter;
using NewCore.Base.Interface.Additionally;

namespace NewCore.Base.Interface.Main
{
  /// <summary>
  /// Интерфейс для быстрого измерителя
  /// </summary>
  public interface IFastMeter : IAttachableDevice
  {
    public IAcVoltageMeasurement AcVoltageManager { get; set; }
    public ICapacitanceMeasurement CapacitanceManager { get; set; }
    public ICommunication CommunicationManager { get; set; }
    public IConnection ConnectionManager { get; set; }
    public IContinuityMeasurement ContinuityManager { get; set; }
    public IDcVoltageMeasurement DcVoltageManager { get; set; }
    public IResistanceMeasurement ResistanceManager { get; set; }

  }
}
