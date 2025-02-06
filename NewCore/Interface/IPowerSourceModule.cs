using NewCore.Base;

namespace NewCore.Interface
{
  /// <summary>
  /// Интерфейс для модуля источника напряжения и тока
  /// </summary>
  public interface IPowerSourceModule : IDevice
  {
    /// <summary>
    /// Номер менеджера шасси.
    /// </summary>
    public int NumberChassis { get; set; }
  }
}
