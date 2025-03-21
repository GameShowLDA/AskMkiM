using NewCore.Base.Device;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Enum;
using NewCore.Function.ModuleRelayControl;

namespace NewCore.Device
{
  /// <summary>
  /// Модуль коммутации реле, обеспечивающее подключение объектов контроля.
  /// </summary>
  public class ModuleRelayControl : DeviceWithIP, IRelaySwitchModule
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ModuleRelayControl"/>.
    /// </summary>
    public ModuleRelayControl()
    {
      BusManager = new BusManager(this);
      MeterManager = new MeterManager(this);
      PointManager = new PointManager(this);
      StateManager = new StateManager(this);

      DeviceType = DeviceEnum.DeviceType.RelaySwitchModule;
      Name = "Модуль коммутации реле 350";
      Description = "Добавить описание сюда";
      PointCount = 350;
      DeviceClass = GetType().FullName;
    }

    /// <inheritdoc />
    public int NumberRack { get; set; }

    /// <inheritdoc />
    public int NumberChassis { get; set; }

    /// <inheritdoc />
    public int PointCount { get; set; }

    /// <inheritdoc />
    public IBusManager BusManager { get; set; }

    /// <inheritdoc />
    public IMeterManager MeterManager { get; set; }

    /// <inheritdoc />
    public IPointManager PointManager { get; set; }

    /// <inheritdoc />
    public IStateManager StateManager { get; set; }
  }
}
