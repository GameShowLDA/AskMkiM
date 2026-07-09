using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands.Connected;
using Ask.Device.Communication.Ethernet.Udp.Protocols;
using Ask.Device.Runtime.Base.DeviceResponses;
using Ask.Device.Runtime.Device;
using System.Net;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Base.Device
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

    public IpConnectedProfile ConnectedProfile { get; } = new IpConnectedProfile();


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
    public ConnectionType ConnectionType { get; init; }
    public IConnectionInfo ConnectionInfo { get; set; }

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
        DeviceProtocol ??= new UdpProtocol(this);
      }
      else
      {
        IPAddress = IPAddress.None;
        DeviceProtocol = null;
        LogError("Некорректный формат IP-адреса.", isDeviceLog: true);
      }
    }

    virtual public (bool Success, string Message) InitializationValidationDelegate(string result, IDevice _device)
    {
      if (_device is IAttachableDevice attachableDevice)
      {
        BaseResponse baseResponse = BaseResponse.FromJson(result);
        if (baseResponse != null)
        {
          if (baseResponse.NumberChassis == attachableDevice.NumberChassis &&
        baseResponse.NumberDevice == attachableDevice.Number)
          {
            return (true, result);
          }
          else
          {
            string errorMessage = string.Empty;

            if (baseResponse.NumberChassis != attachableDevice.NumberChassis)
            {
              errorMessage += $"Несовпадение по NumberChassis: ожидается {attachableDevice.NumberChassis}, получено {baseResponse.NumberChassis}. ";
            }

            if (baseResponse.NumberDevice != attachableDevice.Number)
            {
              errorMessage += $"Несовпадение по NumberDevice: ожидается {attachableDevice.Number}, получено {baseResponse.NumberDevice}.";
            }

            return (false, errorMessage.Trim());
          }
        }
      }
      else
      {
        return result == "1.0.1" ? (true, string.Empty) : (false, result);
      }

      return (false, result);
    }

    virtual public bool ResetValidationDelegate(string result, IDevice _device)
    {
      if (_device is IAttachableDevice attachableDevice)
      {
        BaseResponse baseResponse = BaseResponse.FromJson(result);
        if (baseResponse != null)
        {
          if (baseResponse.NumberChassis == attachableDevice.NumberChassis &&
        baseResponse.NumberDevice == attachableDevice.Number && baseResponse.Answer.Contains("2.0"))
          {
            return true;
          }
          else
          {
            string errorMessage = string.Empty;

            if (baseResponse.NumberChassis != attachableDevice.NumberChassis)
            {
              errorMessage += $"Несовпадение по NumberChassis: ожидается {attachableDevice.NumberChassis}, получено {baseResponse.NumberChassis}. ";
            }

            if (baseResponse.NumberDevice != attachableDevice.Number)
            {
              errorMessage += $"Несовпадение по NumberDevice: ожидается {attachableDevice.Number}, получено {baseResponse.NumberDevice}.";
            }

            return false;
          }
        }
      }
      else
      {
        return result == "2.0.1";
      }

      return false;
    }
  }
}
