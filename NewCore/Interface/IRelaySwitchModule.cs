using NewCore.Base;

namespace NewCore.Interface
{
  /// <summary>
  /// Интерфейс для модуля коммутации реле
  /// </summary>
  public interface IRelaySwitchModule : IDevice
  {
    /// <summary>
    /// Номер менеджера шасси.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Количество точек модуля.
    /// </summary>
    public int PointCount { get; set; }

    public int NumberRack { get; set; }
  }
}
