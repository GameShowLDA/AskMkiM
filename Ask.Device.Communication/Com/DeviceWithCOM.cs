using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.IO.Ports;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Com
{
  /// <summary>
  /// Абстрактный класс <see cref="DeviceWithCOM"/> представляет базовый функционал устройства, 
  /// подключаемого через COM-порт.
  /// </summary>
  /// <remarks>
  /// Этот класс реализует интерфейс <see cref="IDevice"/> и предоставляет базовые методы для подключения 
  /// и отключения устройств через последовательный порт (COM).
  /// </remarks>
  public abstract class DeviceWithCOM : IDevice
  {
    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <summary>
    /// Получает или задает COM-порт, используемый для подключения устройства.
    /// </summary>
    /// <value>Экземпляр класса <see cref="SerialPort"/>, представляющий COM-порт.</value>
    public SerialPort COMPort
    {
      get => _comPort;
      set
      {
        if (_comPort != null)
        {
          DeviceManager.DisableDevice(_comPort.PortName);
        }

        LogWarning($"[{Name}] COMPort меняется: {_comPort?.PortName ?? "null"} → {value?.PortName ?? "null"}", isDeviceLog: true);
        _comPort = value;

      }
    }

    private SerialPort _comPort;

    /// <summary>
    /// Получает или задает идентификатор производителя устройства (Vendor ID).
    /// </summary>
    /// <value>VID устройства в виде строки.</value>
    public string VID { get; set; }

    /// <summary>
    /// Получает или задает идентификатор продукта устройства (Product ID).
    /// </summary>
    /// <value>PID устройства в виде строки.</value>
    public string PID { get; set; }

    /// <inheritdoc />
    public int Number { get; set; }

    private string _connectionDetails;

    /// <inheritdoc />
    public string ConnectionDetails
    {
      get => _connectionDetails;
      set
      {
        _connectionDetails = value;
        if (COMPort?.IsOpen == true)
        {
          LogWarning($"[{Name}] ConnectionDetails изменён при открытом порте — игнор.", isDeviceLog: true);
          return;
        }
        var port = SerialPortCustom.ToObject(value);

        if (port != null)
        {
          COMPort = port;
          DeviceProtocol = new SerialDeviceProtocol(this, COMPort);
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

    /// <inheritdoc />
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// Получает или задает флаг, указывающий, является ли устройство подключаемым.
    /// </summary>
    public bool IsAttachableDevice { get; set; }

    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public string DeviceClass { get; set; }

    /// <summary>
    /// Получает или задает скорость передачи данных (Baud Rate) для COM-порта.
    /// </summary>
    /// <value>Скорость передачи данных в бит/с. Обычно по умолчанию 9600.</value>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// Получает или задает количество стоповых бит для COM-порта.
    /// </summary>
    /// <value>Стоповые биты, тип <see cref="StopBits"/>. Обычно по умолчанию <see cref="StopBits.One"/>.</value>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>
    /// Получает или задает количество бит данных для COM-порта.
    /// </summary>
    /// <value>Количество бит данных. Обычно по умолчанию 8.</value>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// Получает или задает режим чётности для COM-порта.
    /// </summary>
    /// <value>Чётность, тип <see cref="Parity"/>. Обычно по умолчанию <see cref="Parity.None"/>.</value>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    /// Получает или задает режим управления потоком для COM-порта.
    /// </summary>
    /// <value>Режим управления потоком в виде строки (например, "Xon/Xoff", "Аппаратное", "Нет").</value>
    public string FlowControl { get; set; } = "Нет";

    /// <inheritdoc />
    public IConnectable ConnectableManager { get; set; }

    /// <inheritdoc />
    public IDeviceProtocol DeviceProtocol { get; set; }
  }
}
