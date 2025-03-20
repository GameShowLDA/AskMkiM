using NewCore.Base.Device;
using NewCore.Base.Function.ManagerChassis;

namespace NewCore.Base.Interface.Main
{
  /// <summary>
  /// Интерфейс для менеджера шасси.
  /// </summary>
  public interface IChassisManager : IDevice
  {
    /// <summary>
    /// Управление состоянием шасси.
    /// </summary>
    public IStateManagerChassis StateManager { get; set; }

    /// <summary>
    /// Управление питанием шасси.
    /// </summary>
    public IPowerManagerChassis PowerManager { get; set; }
  }
}
