using System.Net;
using System.Net.Sockets;
using NewCore.Base.Device;
using NewCore.Base.Function.FastMeter;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Enum;
using NewCore.Function.Keysight3466new;
using NewCore.FunctionAdapters.Keysight3466new;

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
    public IResistanceMeasurement ResistanceManager { get; set; }

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
      Name = "Keysight 3466 new";
      Description = "Реализовать описание в NewCore.Device.KeysightDevice";
      DeviceClass = GetType().FullName;
      DeviceType = DeviceEnum.DeviceType.FastMeter;
      IsConnected = false;

      CapacitanceManager = new CapacitanceMeasurementAdapter(this);
      ConnectableManager = new KeysightConnectionAdapter(this);
      ContinuityManager = new ContinuityMeasurementAdapter(this);
      ResistanceManager = new ResistanceMeasurementAdapter(this);
      AcVoltageManager = new AcVoltageMeasurementAdapter(this);
      DcVoltageManager = new DcVoltageMeasurementAdapter(this);
      DeviceProtocol = new KeysightDeviceProtocol(this, Port);
    }
  }
}
