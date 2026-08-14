using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Metadata.Commands.MultimeterCommands;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Ethernet.Tcp.Protocols;
using Ask.Device.Communication.Common;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.Base.Connected;
using Ask.Device.Runtime.Function.Base.Multimeter.Measurements;
using Ask.Device.Runtime.Function.Base.Multimeter.SelfCheck;
using System.Net;

namespace Ask.Device.Runtime.Device
{
  /// <summary>
  /// Устройство Keysight 3466, предназначенное для измерения различных электрических параметров.
  /// Работает через сетевое подключение (TCP/IP).
  /// </summary>
  public class KeysightDevice : DeviceWithTcpIp, IMultimeter
  {
    /// <inheritdoc />
    public int NumberChassis { get; set; }

    /// <inheritdoc />
    public ICapacitanceMeasurement CapacitanceManager { get; set; }

    /// <inheritdoc />
    public IContinuityMeasurement ContinuityManager { get; set; }

    /// <inheritdoc />
    public IAcVoltageMeasurement AcVoltageManager { get; set; }

    /// <inheritdoc />
    public IDcVoltageMeasurement DcVoltageManager { get; set; }

    /// <inheritdoc />
    public IDiodeMeasurement DiodeManager { get; set; }

    /// <inheritdoc />
    public IResistanceMeasurement ResistanceManager { get; set; }

    /// <inheritdoc />
    public ITextMessage TextMessage { get; set; }

    /// <inheritdoc />
    public int MaxContinuityResistance { get; set; }

    /// <inheritdoc />
    public double AcwPpuDividerCoefficientPercent { get; set; }

    /// <inheritdoc />
    public double DcwPpuDividerCoefficientPercent { get; set; }

    /// <inheritdoc />
    public MultimeterTypeMode TypeMode { get; set; }

    /// <summary>
    /// Менеджер выполнения самотестирования мультиметра.
    /// </summary>
    public ISelfTestCheckerMultimeter SelfTestManager { get; set; }

    /// <summary>
    /// Профиль команд измерения электрического сопротивления.
    /// </summary>
    public ResistanceMeasurementProfile ResistanceCommands { get; set; }

    /// <summary>
    /// Профиль команд измерения переменного напряжения.
    /// </summary>
    public ACVMeasurementProfile ACVCommands { get; set; }

    /// <summary>
    /// Профиль команд измерения постоянного напряжения.
    /// </summary>
    public DCVMeasurementProfile DCVCommands { get; set; }

    /// <summary>
    /// Профиль команд измерения ёмкости.
    /// </summary>
    public CapacitanceMeasurementProfile CapacitanceCommands { get; set; }

    /// <summary>
    /// Профиль команд режима прозвонки.
    /// </summary>
    public ContinuityMeasurementProfile ContinuityCommands { get; set; }

    /// <summary>
    /// Профиль команд проверки диодов.
    /// </summary>
    public DiodeMeasurementProfile DiodeCommands { get; set; }


    /// <summary>
    /// Устройство Keysight 3466, предназначенное для измерения различных электрических параметров.
    /// Работает через сетевое подключение (TCP/IP).
    /// </summary>
    /// <param name="ip">IP-адрес устройства.</param>
    public KeysightDevice(IPAddress ip)
        : this() => IPAddress = ip;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="KeysightDevice"/>.
    /// </summary>
    public KeysightDevice()
    {
      Name = "Keysight 34465A";
      Description = "Реализовать описание в Ask.Device.Runtime.Device.KeysightDevice";
      DeviceClass = GetType().FullName;
      DeviceType = DeviceType.FastMeter;
      ConnectionInfo.IsConnected = false;
      ConnectedProfile.Port = 5025;
      ConnectedProfile.InitialBeeperDisableCommands = ["SYST:BEEP:STAT OFF"];

      CapacitanceManager = new CapacitanceMeasurementBase(this);
      ConnectableManager = new Transport(this);
      ContinuityManager = new ContinuityMeasurementBase(this);
      ResistanceManager = new ResistanceMeasurementBase(this);
      AcVoltageManager = new ACVMeasurementBase(this);
      DcVoltageManager = new DCVMeasurementBase(this);
      TextMessage = new Function.Keysight3466new.TextMessage(this);
      DiodeManager = new DiodeMeasurementBase(this);
      SelfTestManager = new SelfTestManager();
      DeviceProtocol = new HardwareWatchdogProtocol(
        new TcpProtocol(this, ConnectedProfile.Port),
        Name);

      ResistanceCommands = new ResistanceMeasurementProfile()
      {
        SupportedRanges = new[] { 100d, 1_000d, 10_000d, 100_000d, 1_000_000d, 10_000_000d, 100_000_000d, 1_000_000_000d },
      };
      ACVCommands = new ACVMeasurementProfile()
      {
        SupportedRanges = new[] { 0.1d, 1d, 10d, 100d, 750d },
      };
      DCVCommands = new DCVMeasurementProfile()
      {
        SupportedRanges = new[] { 0.1d, 1d, 10d, 100d, 1000d },
      };
      CapacitanceCommands = new CapacitanceMeasurementProfile()
      {
        SupportedRanges = new[] { 1d, 10d, 100d, 1_000d, 10_000d, 100_000d, 1_000_000d, 10_000_000d },
      };
      ContinuityCommands = new ContinuityMeasurementProfile();
      DiodeCommands = new DiodeMeasurementProfile();


      MaxContinuityResistance = 100000;
      AcwPpuDividerCoefficientPercent = 100d;
      DcwPpuDividerCoefficientPercent = 100d;
    }

    /// <summary>
    /// Преобразует текущий экземпляр мультиметра в объект передачи данных.
    /// </summary>
    /// <returns>Объект <see cref="FastMeterDto"/> с параметрами мультиметра.</returns>
    public FastMeterDto Convert()
    {
      return new FastMeterDto
      {
        Id = Id,
        NumberChassis = NumberChassis,
        Name = Name ?? string.Empty,
        Description = Description ?? string.Empty,
        Number = Number,
        ConnectionDetails = ConnectionDetails ?? string.Empty,
        DeviceType = DeviceType,
        DeviceClass = DeviceClass ?? string.Empty,
        TypeMode = TypeMode,
        MaxContinuityResistance = MaxContinuityResistance,
        AcwPpuDividerCoefficientPercent = AcwPpuDividerCoefficientPercent,
        DcwPpuDividerCoefficientPercent = DcwPpuDividerCoefficientPercent
      };
    }
  }
}
