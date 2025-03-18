using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Main;
using NewCore.Enum;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность устройства коммутации.
  /// </summary>
  public class SwitchingDeviceEntity : ISwitchingDevice
  {
    public int Id { get; set; }
    /// <summary>
    /// Номер менеджера шасси, к которому подключено устройство коммутации.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Название устройства коммутации.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Описание устройства коммутации.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Уникальный номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Детали подключения устройства (IP-адрес, COM-порт и т. д.).
    /// </summary>
    public string ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства, всегда SwitchingDevice.
    /// </summary>
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.SwitchingDevice;

    public string DeviceClass { get; set; }

    [NotMapped]
    public IBusDeviceBusCommutation BusManager { get; set; }

    [NotMapped]
    public ICapacitorDeviceBusCommutation CapacitorManager { get; set; }

    [NotMapped]
    public IConnectorDeviceBusCommutation ConnectorManager { get; set; }

    [NotMapped]
    public IRelayDeviceBusCommutation RelayManager { get; set; }

    [NotMapped]
    public IResistorDeviceBusCommutation ResistorManager { get; set; }

    [NotMapped]
    public IStateDeviceBusCommutation StateManager { get; set; }
    [NotMapped]
    public ISelfTestChecker SelfTestManager { get; set; }


    /// <summary>
    /// Метод инициализации устройства коммутации.
    /// </summary>
    /// <returns>Возвращает true, если инициализация прошла успешно.</returns>
    public async Task<bool> Initialize()
    {
      if (IPAddress.TryParse(ConnectionDetails, out IPAddress address))
      {
        var connect = await NewCore.Communication.DeviceCommandSender.PingAsync("УКШ", address);
        return connect;
      }

      throw new NotFiniteNumberException();
    }
  }
}
