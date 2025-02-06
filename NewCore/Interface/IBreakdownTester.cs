using NewCore.Base;

namespace NewCore.Interface
{
  /// <summary>
  /// Интерфейс для пробойной установки
  /// </summary>
  public interface IBreakdownTester : IDevice
  {
    /// <summary>
    /// Номер менеджера шасси.
    /// </summary>
    public int NumberChassis { get; set; }
  }
}
