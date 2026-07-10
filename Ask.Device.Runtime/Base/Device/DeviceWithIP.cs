using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Ethernet.Udp.Protocols;
using Ask.Device.Runtime.Base.DeviceResponses;
using System.Net;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Base.Device
{
  /// <summary>
  /// Представляет базовый тип устройства, подключаемого по IP-сети.
  /// </summary>
  public abstract class DeviceWithIP : IDevice
  {
    #region IDevice
    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;

    /// <inheritdoc />
    public int Number { get; set; }

    /// <inheritdoc />
    public DeviceType DeviceType { get; set; }

    /// <inheritdoc />
    public string DeviceClass { get; set; } = string.Empty;

    /// <inheritdoc />
    public IConnectable ConnectableManager { get; set; } = null!;

    /// <inheritdoc />
    public IDeviceProtocol DeviceProtocol { get; set; } = null!;

    /// <inheritdoc />
    public IConnectionInfo ConnectionInfo { get; init; }

    /// <inheritdoc />
    public string ConnectionDetails
    {
      get => GetIPAddress(IPAddress);
      set => SetIPAddress(value);
    }

    #endregion

    /// <summary>
    /// Получает или задаёт IP-адрес устройства.
    /// </summary>
    public IPAddress IPAddress { get; set; } = IPAddress.None;

    /// <summary>
    /// Получает или задаёт признак подключения устройства в составе стенда.
    /// </summary>
    public bool IsAttachableDevice { get; set; }

    public IpConnectedProfile ConnectedProfile { get; } = new IpConnectedProfile();


    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceWithIP"/> с заданным IP-адресом.
    /// </summary>
    /// <param name="ipAddress">IP-адрес устройства.</param>
    protected DeviceWithIP(IPAddress ipAddress) : this()
    {
      IPAddress = ipAddress;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceWithIP"/>.
    /// </summary>
    protected DeviceWithIP()
    { }

    /// <summary>
    /// Получает или задаёт менеджер подключения устройства.
    /// </summary>

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

    /// <summary>
    /// Проверяет корректность ответа устройства на команду инициализации.
    /// </summary>
    /// <param name="result">Ответ устройства.</param>
    /// <param name="_device">Экземпляр устройства.</param>
    /// <returns>
    /// Кортеж, содержащий:
    /// <list type="bullet">
    /// <item><description><c>Success</c> — признак успешной проверки.</description></item>
    /// <item><description><c>Message</c> — сообщение об ошибке или исходный ответ устройства.</description></item>
    /// </list>
    /// </returns>
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

    /// <summary>
    /// Проверяет корректность ответа устройства на команду сброса.
    /// </summary>
    /// <param name="result">Ответ устройства.</param>
    /// <param name="_device">Экземпляр устройства.</param>
    /// <returns>
    /// <see langword="true"/>, если ответ соответствует ожидаемому;
    /// иначе <see langword="false"/>.
    /// </returns>
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
