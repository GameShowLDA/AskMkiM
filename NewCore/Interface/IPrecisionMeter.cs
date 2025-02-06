using NewCore.Base;

namespace NewCore.Interface
{
  /// <summary>
  /// Интерфейс для точного измерителя
  /// </summary>
  public interface IPrecisionMeter : IDevice
  {
    /// <summary>
    /// Номер менеджера шасси.
    /// </summary>
    public int NumberChassis { get; set; }
  }

}
