using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.AskMkiM.Base.Commands;
using Ask.Device.Runtime.AskMkiM.Function.ModuleRelayControl;
using Ask.Device.Runtime.AskMkiM.Function.ModuleRelayControl.SelfCheck;
using Ask.Device.Runtime.Base.Connected;
using Ask.Device.Runtime.Base.DeviceProtocol;

namespace Ask.Device.Runtime.Device.RelaySwitchModule
{
  /// <summary>
  /// Модуль коммутации реле, обеспечивающее подключение объектов контроля.
  /// </summary>
  public class ModuleRelayControl300 : DeviceWithUdpIp, IRelaySwitchModule
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ModuleRelayControl300"/>.
    /// </summary>
    public ModuleRelayControl300()
    {
      ConnectedProfile.Initialize = new DeviceCommand(1, 0, 0, 0).ToString();
      DeviceType = DeviceType.RelaySwitchModule;
      Name = "Модуль МКР-300";
      Description = "Добавить описание сюда";
      PointCount = 300;
      DeviceClass = GetType().FullName;

      ConnectableManager = new Transport(this);
      BusManager = new BusManager(this);
      MeterManager = new MeterManager(this);
      PointManager = new PointManager(this);
      SelfTestManager = new SelfTestManager(this);
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

    public RelaySwitchModuleDto Convert()
    {
      return new RelaySwitchModuleDto
      {
        Id = Id,
        NumberChassis = NumberChassis,
        NumberRack = NumberRack,
        PointCount = PointCount,
        Name = Name ?? string.Empty,
        Description = Description ?? string.Empty,
        Number = Number,
        ConnectionDetails = ConnectionDetails ?? string.Empty,
        DeviceType = DeviceType,
        DeviceClass = DeviceClass ?? string.Empty,
        BusType = BusType,
        SwitchResistance = SwitchResistance,
        SwitchCapacitance = SwitchCapacitance
      };
    }
  }
}

