using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.Status;

namespace Ask.Device.Runtime.Base.DeviceProtocol
{
  /// <summary>
  /// Представляет базовый тип устройства, подключаемого по USB.
  /// </summary>
  public abstract class DeviceWithUSB : IDevice
  {
    public DeviceWithUSB()
    {
      ConnectionInfo = new ConnectionInfoBase(this, ConnectionType.USB);
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
      }
    }

    #endregion

    /// <summary>
    /// Тип подключения устройства.
    /// </summary>
    public ConnectionType ConnectionType { get; init; } = ConnectionType.USB;

    /// <summary>
    /// Строка с дополнительной информацией о подключении устройства.
    /// </summary>
    private string _connectionDetails = string.Empty;

    /// <summary>
    /// Профиль параметров подключения устройства по USB.
    /// </summary>
    public UsbConnectedProfile ConnectedProfile { get; set; } = new UsbConnectedProfile();
  }
}

