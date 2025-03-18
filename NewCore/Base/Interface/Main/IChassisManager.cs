using NewCore.Base.Device;
using NewCore.Base.Function.ManagerChassis;

namespace NewCore.Base.Interface.Main
{
  /// <summary>
  /// Интерфейс для менеджера шасси
  /// </summary>
  public interface IChassisManager : IDevice
  {
    public IStateManagerChassis StateManager { get; set; }
    public IPowerManagerChassis PowerManager { get; set; }

  }
}
