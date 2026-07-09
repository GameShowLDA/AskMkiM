using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands.Connected;
using Ask.Device.Communication.Ethernet.Tcp.Protocols;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.Connected;
using Ask.Device.Runtime.Function.Multimeter.Measurements;
using System.Net;
using System.Net.Sockets;

namespace Ask.Device.Runtime.Device
{
  /// <summary>
  /// Устройство Keysight 3466, предназначенное для измерения различных электрических параметров.
  /// Работает через сетевое подключение (TCP/IP).
  /// </summary>
  public class KeysightDevice : DeviceWithIP, IMultimeter
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
    public ITextMessage TextMessage { get; set; }

    /// <inheritdoc />
    public int MaxContinuityResistance { get; set; }

    /// <inheritdoc />
    public double AcwPpuDividerCoefficientPercent { get; set; }

    /// <inheritdoc />
    public double DcwPpuDividerCoefficientPercent { get; set; }

    /// <inheritdoc />
    public MultimeterTypeMode TypeMode { get; set; }
    public ISelfTestCheckerMultimeter SelfTestManager { get; set; }
    public ResistanceMeasurementProfile ResistanceCommands { get; set; }
    public ACVMeasurementProfile ACVCommands { get; set; }
    public DCVMeasurementProfile DCVCommands { get; set; }
    public CapacitanceMeasurementProfile CapacitanceCommands { get; set; }
    public ContinuityMeasurementProfile ContinuityCommands { get; set; }
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
      ConnectionInfo.IsConnected= false;
      ConnectionType = ConnectionType.IP_TCP;
      ConnectedProfile.Port = 5025;

      CapacitanceManager = new CapacitanceMeasurementBase(this);
      ConnectableManager = new Transport(this);
      ContinuityManager = new ContinuityMeasurementBase(this);
      ResistanceManager = new ResistanceMeasurementBase(this);
      AcVoltageManager = new ACVMeasurementBase(this);
      DcVoltageManager = new DCVMeasurementBase(this);
      TextMessage = new Function.Keysight3466new.TextMessage(this);
      DiodeManager = new DiodeMeasurementBase(this);
      SelfTestManager = new Function.Multimeter.SelfCheck.SelfTestManager();
      DeviceProtocol = new TcpProtocol(this, ConnectedProfile.Port);

      ResistanceCommands = new ResistanceMeasurementProfile();
      ACVCommands = new ACVMeasurementProfile();
      DCVCommands = new DCVMeasurementProfile();
      CapacitanceCommands = new CapacitanceMeasurementProfile();
      ContinuityCommands = new ContinuityMeasurementProfile();
      DiodeCommands = new DiodeMeasurementProfile();


      MaxContinuityResistance = 100000;
      AcwPpuDividerCoefficientPercent = 100d;
      DcwPpuDividerCoefficientPercent = 100d;
    }

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
