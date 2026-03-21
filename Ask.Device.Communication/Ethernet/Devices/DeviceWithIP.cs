using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Ethernet.Udp;
using System.Net;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Ethernet
{
  /// <summary>
  /// Представляет базовый тип устройства, подключаемого по IP-сети.
  /// </summary>
  public abstract class DeviceWithIP : IDevice
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
    /// Получает или задаёт IP-адрес устройства.
    /// </summary>
    public IPAddress IPAddress { get; set; } = IPAddress.None;

    /// <summary>
    /// Получает или задаёт номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Получает или задаёт идентификатор устройства.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Получает или задаёт полное имя CLR-типа устройства.
    /// </summary>
    public string DeviceClass { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт строку с параметрами подключения устройства.
    /// </summary>
    public string ConnectionDetails
    {
      get => GetIPAddress(IPAddress);
      set => SetIPAddress(value);
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
    /// Инициализирует новый экземпляр <see cref="DeviceWithIP"/> с заданным IP-адресом.
    /// </summary>
    /// <param name="ipAddress">IP-адрес устройства.</param>
    public DeviceWithIP(IPAddress ipAddress)
    {
      IPAddress = ipAddress;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceWithIP"/>.
    /// </summary>
    public DeviceWithIP()
    {
    }

    /// <summary>
    /// Получает или задаёт менеджер подключения устройства.
    /// </summary>
    public IConnectable ConnectableManager { get; set; } = null!;

    /// <summary>
    /// Получает или задаёт транспортный протокол устройства.
    /// </summary>
    public IDeviceProtocol DeviceProtocol { get; set; } = null!;

    /// <summary>
    /// Возвращает строковое представление IP-адреса.
    /// </summary>
    /// <param name="ipAddress">IP-адрес для преобразования.</param>
    /// <returns>Строковое представление IP-адреса.</returns>
    internal string GetIPAddress(IPAddress ipAddress)
    {
      return ipAddress.ToString();
    }

    /// <summary>
    /// Устанавливает IP-адрес из строки и инициализирует UDP-протокол при валидном адресе.
    /// </summary>
    /// <param name="ipString">Строковое представление IP-адреса.</param>
    internal void SetIPAddress(string ipString)
    {
      if (IPAddress.TryParse(ipString, out IPAddress? ipAddress))
      {
        IPAddress = ipAddress;
        DeviceProtocol ??= new UdpDeviceProtocol(this);
      }
      else
      {
        IPAddress = IPAddress.None;
        DeviceProtocol = null;
        LogError("Некорректный формат IP-адреса.", isDeviceLog: true);
      }
    }
  }
}
