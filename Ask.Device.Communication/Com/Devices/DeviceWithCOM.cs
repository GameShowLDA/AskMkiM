using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Com.Configuration;
using Ask.Device.Communication.Com.Interop;
using Ask.Device.Communication.Com.Protocols;
using System.IO.Ports;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Com.Devices
{
  /// <summary>
  /// Представляет базовый тип устройства, подключаемого через COM-порт.
  /// </summary>
  public abstract class DeviceWithCOM : IDevice
  {
    /// <summary>
    /// Получает или задаёт имя устройства.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт описание устройства.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт COM-порт, используемый для подключения устройства.
    /// </summary>
    public SerialPort COMPort
    {
      get => _comPort;
      set
      {
        if (_comPort != null)
        {
          ComPortDeviceManager.DisableDevice(_comPort.PortName);
        }

        LogWarning($"[{Name}] COMPort меняется: {_comPort?.PortName ?? "null"} → {value?.PortName ?? "null"}", isDeviceLog: true);
        _comPort = value;
      }
    }

    /// <summary>
    /// Хранит текущий экземпляр COM-порта устройства.
    /// </summary>
    private SerialPort _comPort = null!;

    /// <summary>
    /// Получает или задаёт идентификатор производителя устройства.
    /// </summary>
    public string VID { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт идентификатор продукта устройства.
    /// </summary>
    public string PID { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт порядковый номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Хранит сериализованные параметры подключения.
    /// </summary>
    private string _connectionDetails = string.Empty;

    /// <summary>
    /// Получает или задаёт сериализованное описание подключения к COM-порту.
    /// </summary>
    public string ConnectionDetails
    {
      get => _connectionDetails;
      set
      {
        _connectionDetails = value;
        if (COMPort?.IsOpen == true)
        {
          LogWarning($"[{Name}] ConnectionDetails изменён при открытом порте, изменение параметров игнорируется.", isDeviceLog: true);
          return;
        }

        var port = SerialPortCustom.ToObject(value);

        if (port != null)
        {
          COMPort = port;
          DeviceProtocol = new ComProtocol(this, port);
          LogInformation($"[{Name}] COM-порт сконфигурирован из ConnectionDetails и протокол установлен.", isDeviceLog: true);
        }
        else
        {
          LogWarning($"[{Name}] ConnectionDetails={value} → COM-порт будет сброшен в null", isDeviceLog: true);
          COMPort = null;
          DeviceProtocol = null;
        }
      }
    }

    /// <summary>
    /// Получает или задаёт тип устройства.
    /// </summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// Получает или задаёт признак подключения устройства в составе стенда.
    /// </summary>
    public bool IsAttachableDevice { get; set; }

    /// <summary>
    /// Получает или задаёт идентификатор устройства.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Получает или задаёт полное имя CLR-типа устройства.
    /// </summary>
    public string DeviceClass { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт скорость передачи данных для COM-порта.
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// Получает или задаёт количество стоп-бит для COM-порта.
    /// </summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>
    /// Получает или задаёт количество бит данных для COM-порта.
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// Получает или задаёт режим чётности для COM-порта.
    /// </summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    /// Получает или задаёт режим управления потоком для COM-порта.
    /// </summary>
    public string FlowControl { get; set; } = "Нет";

    /// <summary>
    /// Получает или задаёт менеджер подключения устройства.
    /// </summary>
    public IConnectable ConnectableManager { get; set; } = null!;

    /// <summary>
    /// Получает или задаёт транспортный протокол устройства.
    /// </summary>
    public IDeviceProtocol DeviceProtocol { get; set; } = null!;
  }
}
