using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Additionally;

namespace NewCore.Base.Interface.Main
{
  /// <summary>
  /// Интерфейс для модуля коммутации реле.
  /// </summary>
  public interface IRelaySwitchModule : IAttachableDevice
  {
    /// <summary>
    /// Количество точек модуля.
    /// </summary>
    public int PointCount { get; set; }

    /// <summary>
    /// Менеджер для управления коммутацией шин.
    /// </summary>
    IBusManager BusManager { get; set; }

    /// <summary>
    /// Менеджер для управления измерителем.
    /// </summary>
    IMeterManager MeterManager { get; set; }

    /// <summary>
    /// Менеджер для управления реле и точками подключения.
    /// </summary>
    IPointManager PointManager { get; set; }
  }
}
