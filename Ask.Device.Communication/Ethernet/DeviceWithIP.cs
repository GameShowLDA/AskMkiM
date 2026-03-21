using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Ethernet.Udp;
using System.Net;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Ethernet
{
  /// <summary>
  /// Абстрактный класс, представляющий устройство с подключением по IP-адресу.
  /// </summary>
  public abstract class DeviceWithIP : IDevice
  {
    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <summary>
    /// IP-адрес устройства.
    /// </summary>
    public IPAddress IPAddress { get; set; }

    /// <inheritdoc />
    public int Number { get; set; }

    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public string DeviceClass { get; set; }

    /// <summary>
    /// Строка с данными о подключении (возвращает строковое представление IP-адреса).
    /// </summary>
    public string ConnectionDetails
    {
      get => GetIPAddress(IPAddress);
      set => SetIPAddress(value);
    }

    /// <inheritdoc />
    public DeviceType DeviceType { get; set; }

    /// <inheritdoc />
    public bool IsAttachableDevice { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceWithIP"/> с указанным IP-адресом.
    /// </summary>
    /// <param name="iPAddress">IP-адрес устройства.</param>
    public DeviceWithIP(IPAddress iPAddress)
    {
      IPAddress = iPAddress;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceWithIP"/> без указания IP-адреса.
    /// </summary>
    public DeviceWithIP()
    {
    }

    /// <inheritdoc />
    public IConnectable ConnectableManager { get; set; }

    /// <inheritdoc />
    public IDeviceProtocol DeviceProtocol { get; set; }

    /// <summary>
    /// Получает строковое представление указанного IP-адреса.
    /// </summary>
    /// <param name="iPAddress">IP-адрес.</param>
    /// <returns>Строковое представление IP-адреса.</returns>
    internal string GetIPAddress(IPAddress iPAddress)
    {
      return iPAddress.ToString();
    }

    /// <summary>
    /// Устанавливает IP-адрес из строки.
    /// Если строка невалидна, возвращает <see cref="IPAddress.None"/>.
    /// </summary>
    /// <param name="ipString">Строка ip-адреса.</param>
    internal void SetIPAddress(string ipString)
    {
      if (IPAddress.TryParse(ipString, out IPAddress ipAddress))
      {
        IPAddress = ipAddress;
        if (DeviceProtocol == null)
        {
          DeviceProtocol = new UdpDeviceProtocol(this);
        }
      }
      else
      {
        DeviceProtocol = null;
        LogError("Invalid IP Address format.");
      }
    }
  }
}
