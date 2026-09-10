using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Services.Validation.Devices;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Commands;
using Ask.Device.Runtime.Function.Base.Connected;
using Ask.Device.Runtime.Function.ModuleRelayControl.SelfCheck;

namespace Ask.Device.Runtime.Device
{
  /// <summary>
  /// Модуль коммутации реле, обеспечивающее подключение объектов контроля.
  /// </summary>
  public class ModuleRelayControl : DeviceWithUdpIp, IRelaySwitchModule
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ModuleRelayControl"/>.
    /// </summary>
    public ModuleRelayControl()
    {
      ConnectedProfile.Initialize = new DeviceCommand(1, 0, 0, 0).ToString();
      ConnectedProfile.Reset = new DeviceCommand(2, 1, 0, 0).ToString();
      DeviceType = DeviceType.RelaySwitchModule;
      Name = "Модуль МКР";
      Description = "Добавить описание сюда";
      PointCount = 350;
      DeviceClass = GetType().FullName;

      ConnectableManager = new Transport(this);
      BusManager = new Function.ModuleRelayControl.BusManager(this);
      MeterManager = new Function.ModuleRelayControl.MeterManager(this);
      PointManager = new Function.ModuleRelayControl.PointManager(this);
      SelfTestManager = new SelfTestManager(this);
    }

    /// <inheritdoc />
    public int NumberRack { get; set; }

    /// <inheritdoc />
    public int NumberChassis { get; set; }

    /// <summary>
    /// Получает актуальный экземпляр тестера, которому принадлежит модуль.
    /// Задаётся при построении runtime-устройства.
    /// </summary>
    public Func<Task<IChassisManager?>>? ChassisManagerProvider { get; set; }

    /// <summary>
    /// Проверяет, блокирует ли симуляция сбоя тестера ответы этого МКР на любые команды.
    /// </summary>
    internal async Task<bool> ShouldSimulateChassisFailureAsync()
    {
      if (!ExecutionConfig.GetIsIdleModeEnabled()
        || EquipmentExecutionContext.IsSelfTest
        || ChassisManagerProvider == null)
      {
        return false;
      }

      var chassis = await ChassisManagerProvider();
      return chassis != null
        && chassis.Number == NumberChassis
        && IdleHardwareErrorSimulator.ShouldSimulateHardwareError(chassis);
    }

    /// <inheritdoc />
    public int PointCount
    {
      get => _pointCount;
      set
      {
        if (value is < RelaySwitchModuleConfigurationValidator.MinimumPointCount or > RelaySwitchModuleConfigurationValidator.MaximumPointCount)
        {
          throw new ArgumentOutOfRangeException(nameof(value), value, "Недопустимое количество точек модуля.");
        }

        _pointCount = value;
        if (PointManager is IPointCountReconfigurable reconfigurable)
        {
          reconfigurable.ReconfigurePointCount(value);
        }
      }
    }

    /// <summary>
    /// Количество точек модуля коммутации реле.
    /// </summary>
    private int _pointCount;

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
