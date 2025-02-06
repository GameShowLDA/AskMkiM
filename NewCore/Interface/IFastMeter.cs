using NewCore.Base;

namespace NewCore.Interface
{
  /// <summary>
  /// Интерфейс для быстрого измерителя
  /// </summary>
  public interface IFastMeter : IDevice
  {
    /// <summary>
    /// Номер менеджера шасси.
    /// </summary>
    public int NumberChassis { get; set; }
  }
}
