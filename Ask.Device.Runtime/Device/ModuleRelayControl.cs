using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Ethernet;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.ModuleRelayControl.SelfCheck;

namespace Ask.Device.Runtime.Device
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
      ConnectableManager = new Function.ModuleRelayControl.StateManager(this);
      BusManager = new Function.ModuleRelayControl.BusManager(this);
      MeterManager = new Function.ModuleRelayControl.MeterManager(this);
      PointManager = new Function.ModuleRelayControl.PointManager(this);
      SelfTestManager = new SelfTestManager(this);

      DeviceType = DeviceType.RelaySwitchModule;
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

    /// <inheritdoc />
    public ISelfTestCheckerModuleRelayControl SelfTestManager { get; set; }

    /// <inheritdoc />
    public SwitchingBusNew BusType { get; set; } = SwitchingBusNew.AB1;
    public double SwitchResistance { get; set; }
    public double SwitchCapacitance { get; set; }
  }
}
