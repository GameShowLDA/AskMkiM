using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using NewCore.Base.Device;
using NewCore.Function.ModuleRelayControl.SelfCheck;
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
      ConnectableManager = new StateManagerAdapter(this);
      BusManager = new BusManagerAdapter(this);
      MeterManager = new MeterManagerAdapter(this);
      PointManager = new PointManagerAdapter(this);
      SelfTestManager = new SelfTestManager(this);

      DeviceType = Ask.Core.Shared.Metadata.Enums.DeviceEnums.DeviceType.RelaySwitchModule;
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
    public ISelfTestCheckerModuleRelayControl SelfTestManager { get; set; }
  }
}