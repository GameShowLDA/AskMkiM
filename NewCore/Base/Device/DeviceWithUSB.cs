using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace NewCore.Base.Device
{
  /// <summary>
  /// Абстрактный класс, представляющий устройство с USB-подключением.
  /// </summary>
  public abstract class DeviceWithUSB : IDevice
  {
    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public int Number { get; set; }

    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public string DeviceClass { get; set; }

    /// <inheritdoc />
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// Строка подключения USB-устройства.
    /// Используется как шаблон имени для поиска.
    /// </summary>
    public string ConnectionDetails
    {
      get => _connectionDetails;
      set
      {
        _connectionDetails = value;
      }
    }

    /// <inheritdoc />
    public IConnectable ConnectableManager { get; set; }

    /// <inheritdoc />
    public IDeviceProtocol DeviceProtocol { get; set; }

    private string _connectionDetails = string.Empty;
  }
}
