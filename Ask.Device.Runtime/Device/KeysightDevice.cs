using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Ethernet;
using Ask.Device.Communication.Ethernet.Tcp;
using System.Net;
using System.Net.Sockets;

namespace NewCore.Device
{
  /// <summary>
  /// Устройство Keysight 3466, предназначенное для измерения различных электрических параметров.
  /// Работает через сетевое подключение (TCP/IP).
  /// </summary>
  public class KeysightDevice : DeviceWithIP, IFastMeter
  {
    /// <summary>
    /// IP-адрес устройства.
    /// </summary>
    public IPAddress IP { get; set; }

    /// <summary>
    /// Флаг состояния подключения устройства.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Порт, используемый для связи с устройством (по умолчанию 5025).
    /// </summary>
    public int Port => 5025;

    /// <summary>
    /// TCP-клиент для установления соединения с устройством.
    /// </summary>
    internal TcpClient Client { get; set; }

    /// <summary>
    /// Сетевой поток для передачи команд и получения данных.
    /// </summary>
    internal NetworkStream Stream { get; set; }

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
    public int MaxContinuityResistance { get; set; }
    public MultimeterTypeMode TypeMode { get; set; }

    /// <summary>
    /// Устройство Keysight 3466, предназначенное для измерения различных электрических параметров.
    /// Работает через сетевое подключение (TCP/IP).
    /// </summary>
    /// <param name="ip">IP-адрес устройства.</param>
    public KeysightDevice(IPAddress ip)
        : this() => IP = ip;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="KeysightDevice"/>.
    /// </summary>
    public KeysightDevice()
    {
      Name = "Мультиметр 34465A";
      Description = "Реализовать описание в NewCore.Device.KeysightDevice";
      DeviceClass = GetType().FullName;
      DeviceType = Ask.Core.Shared.Metadata.Enums.DeviceEnums.DeviceType.FastMeter;
      IsConnected = false;

      CapacitanceManager = new Function.Keysight3466new.CapacitanceMeasurement(this);
      ConnectableManager = new Function.Keysight3466new.KeysightConnection(this);
      ContinuityManager = new Function.Keysight3466new.ContinuityMeasurement(this);
      ResistanceManager = new Function.Keysight3466new.ResistanceMeasurement(this);
      AcVoltageManager = new Function.Keysight3466new.AcVoltageMeasurement(this);
      DcVoltageManager = new Function.Keysight3466new.DcVoltageMeasurement(this);
      DiodeManager = new Function.Keysight3466new.DiodeMeasurement(this);
      DeviceProtocol = new TcpProtocol(this, Port);
      MaxContinuityResistance = 100000;
    }
  }
}
