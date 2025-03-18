using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Additionally;

namespace NewCore.Base.Interface.Main
{
  /// <summary>
  /// Интерфейс для модуля источника напряжения и тока
  /// </summary>
  public interface IPowerSourceModule : IAttachableDevice
  {
    public IBusManager BusManager { get; set; }
    public ICurrentManager CurrentManager { get; set; }
    public IStateManager StateManager { get; set; }
    public IVoltageManager VoltageManager{ get; set; }
  }
}
