using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Com.Configuration;
using Ask.Device.Communication.Com.Protocols;
using Ask.Device.Runtime.Function.Base.Status;
using System.IO.Ports;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Base.Device
{
  /// <summary>
  /// Представляет базовый тип устройства, подключаемого через COM-порт.
  /// </summary>
  public abstract class DeviceWithCOM : IDevice, IComPortSettingsProvider
  {
    public DeviceWithCOM()
    {
      ConnectionInfo = new ConnectionInfoBase(this, ConnectionType.COM);
    }

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

    #endregion

    /// <inheritdoc />
    public abstract Ask.Core.Shared.DTO.Devices.Base.ComPortSettings DefaultComPortSettings { get; }

    /// <summary>
    /// Применяет параметры последовательного порта по умолчанию,
    /// если настройки подключения ещё не были сохранены.
    /// </summary>
    protected void ApplyDefaultComPortSettings()
    {
      if (string.IsNullOrWhiteSpace(ConnectionDetails))
      {
        ConnectionDetails = SerialPortCustom.SerializeSettings(DefaultComPortSettings);
      }
    }

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
          if (_comPort.IsOpen)
          {
            _comPort.Close();
          }

          _comPort.Dispose();
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
    /// Хранит сериализованные параметры подключения.
    /// </summary>
    private string _connectionDetails = string.Empty;

    /// <summary>
    /// Получает или задаёт признак подключения устройства в составе стенда.
    /// </summary>
    public bool IsAttachableDevice { get; set; }

    /// <summary>
    /// Профиль параметров подключения устройства по COM-интерфейсу.
    /// </summary>
    public ComConnectedProfile ConnectedProfile { get; set; } = new ComConnectedProfile();
  }
}
