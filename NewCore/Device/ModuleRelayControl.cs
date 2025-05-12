using NewCore.Base.Device;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Enum;
using NewCore.Function.ModuleRelayControl;
using NewCore.FunctionAdapters.ModuleRelayControl;

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
      BusManager = new BusManagerAdapter(this);
      MeterManager = new MeterManagerAdapter(this);
      PointManager = new PointManagerAdapter(this);
      StateManager = new StateManagerAdapter(this);

      DeviceType = DeviceEnum.DeviceType.RelaySwitchModule;
      Name = "Модуль МКР-350";
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

    public IStateManager StateManager { get; set; }
  }
}
