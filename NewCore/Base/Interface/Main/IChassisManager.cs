using NewCore.Base.Device;
using NewCore.Base.Function.ManagerChassis;
using NewCore.Base.Interface.Additionally;

namespace NewCore.Base.Interface.Main
{
  /// <summary>
  /// Интерфейс для менеджера шасси.
  /// </summary>
  public interface IChassisManager : IDevice, IHeadUnit
  {
    /// <summary>
    /// Управление питанием шасси.
    /// </summary>
    public IPowerManagerChassis PowerManager { get; set; }
  }
}
