using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Ethernet.Tcp.Protocols;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.B5108;
using Ask.Device.Runtime.Function.Base.Connected;
using System.Net;
using Ask.Core.Shared.DTO.Devices.PowerSupply;

namespace Ask.Device.Runtime.Device;

/// <summary>
/// Управляет внешним источником питания Б5-108 по LAN/SCPI.
/// </summary>
public sealed class B5108 : DeviceWithTcpIp, IPowerSupply, IDisposable
{
  /// <summary>
  /// Создаёт источник Б5-108 с TCP-портом 80.
  /// </summary>
  public B5108()
  {
    Name = "Б5-108";
    Description = "Внешний источник постоянного напряжения 0–150 В и тока 0–5 А, LAN/SCPI.";
    DeviceClass = GetType().FullName!;
    DeviceType = DeviceType.PowerSupply;
    ConnectedProfile.Port = 80;
    ConnectedProfile.Timeout = 3000;
    ConnectedProfile.Reset = "*RST";
    ConnectedProfile.CheckMode = "B5-108";
    DeviceProtocol = new TcpProtocol(this, ConnectedProfile.Port, readUntilNewline: true);
    VoltageManager = new VoltageManager(this);
    CurrentManager = new CurrentManager(this);
    OutputManager = new OutputManager(this);
    MeasurementManager = new MeasurementManager(this);
    ConnectableManager = new Transport(this);
  }

  /// <summary>
  /// Создаёт источник Б5-108 с заданным IP-адресом.
  /// </summary>
  /// <param name="ipAddress">IP-адрес источника питания.</param>
  public B5108(IPAddress ipAddress) : this() => IPAddress = ipAddress;

  /// <inheritdoc />
  public int NumberChassis { get; set; }

  /// <inheritdoc />
  public IVoltageManager VoltageManager { get; set; }
  /// <inheritdoc />
  public ICurrentManager CurrentManager { get; set; }
  /// <inheritdoc />
  public IOutputManager OutputManager { get; set; }
  /// <inheritdoc />
  public IMeasurementManager MeasurementManager { get; set; }

  /// <inheritdoc />
  public PowerSupplyDto Convert() => new()
  {
    Id = Id,
    NumberChassis = NumberChassis,
    Name = Name,
    Description = Description,
    Number = Number,
    ConnectionDetails = ConnectionDetails,
    DeviceClass = DeviceClass,
    DeviceType = DeviceType,
    IsHardwareFailureSimulationEnabled = IsHardwareFailureSimulationEnabled,
    Port = ConnectedProfile.Port,
  };

  /// <summary>
  /// Закрывает TCP-протокол источника при освобождении runtime-устройства.
  /// </summary>
  public void Dispose() => (DeviceProtocol as IDisposable)?.Dispose();
}
