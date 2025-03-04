using System.Net;
using Utilities;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base
{
  /// <summary>
  /// Абстрактный класс, представляющий устройство с подключением по IP-адресу.
  /// </summary>
  public abstract class DeviceWithIP : IDevice
  {
    /// <summary>
    /// Название устройства.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Описание устройства.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// IP-адрес устройства.
    /// </summary>
    public IPAddress IPAddress { get; set; }

    /// <summary>
    /// Номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Строка с данными о подключении (возвращает строковое представление IP-адреса).
    /// </summary>
    public string ConnectionDetails
    {
      get => GetIPAddress(IPAddress);
      set => SetIPAddress();
    }

    /// <summary>
    /// Тип устройства.
    /// </summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// Выполняет инициализацию устройства.
    /// </summary>
    /// <returns>
    /// Возвращает <c>true</c>, если инициализация прошла успешно, иначе <c>false</c>.
    /// </returns>
    public abstract Task<bool> Initialize();

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
    /// <returns>Объект <see cref="IPAddress"/>.</returns>
    internal IPAddress SetIPAddress()
    {
      var ipString = "192.168.1.1";
      if (IPAddress.TryParse(ipString, out IPAddress ipAddress))
      {
        return ipAddress;
      }
      else
      {
        LoggerUtility.LogError("Invalid IP Address format.");
        return IPAddress.None;
      }
    }
  }
}
