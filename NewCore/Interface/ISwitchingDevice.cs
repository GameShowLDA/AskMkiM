using NewCore.Base;

namespace NewCore.Interface
{
  /// <summary>
  /// Интерфейс для устройства коммутации.
  /// </summary>
  public interface ISwitchingDevice : IDevice
  {
    /// <summary>
    /// Номер менеджера шасси.
    /// </summary>
    public int NumberChassis { get; set; }

  }
}
