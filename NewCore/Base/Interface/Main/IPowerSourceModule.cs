using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Additionally;

namespace NewCore.Base.Interface.Main
{
  /// <summary>
  /// Интерфейс для модуля источника напряжения и тока.
  /// </summary>
  public interface IPowerSourceModule : IAttachableDevice
  {
    /// <summary>
    /// Управление подключением и отключением шин.
    /// </summary>
    public IBusManager BusManager { get; set; }

    /// <summary>
    /// Управление настройками тока.
    /// </summary>
    public ICurrentManager CurrentManager { get; set; }

    /// <summary>
    /// Управление настройками напряжения.
    /// </summary>
    public IVoltageManager VoltageManager { get; set; }
  }
}
