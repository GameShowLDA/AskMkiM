using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using NewCore.Base.Function.FastMeter;
using NewCore.Base.Interface.Main;
using NewCore.Enum;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность быстрого измерителя.
  /// </summary>
  public class FastMeterEntity : IFastMeter
  {
    public int Id { get; set; }
    /// <summary>
    /// Номер менеджера шасси, к которому подключен быстрый измеритель.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Название быстрого измерителя.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Описание быстрого измерителя.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Уникальный номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Детали подключения измерителя (IP-адрес, COM-порт и т. д.).
    /// </summary>
    public string ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства, всегда FastMeter.
    /// </summary>
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.FastMeter;

    public string DeviceClass { get; set; }

    [NotMapped]
    public IAcVoltageMeasurement AcVoltageManager { get; set; }

    [NotMapped]

    public ICapacitanceMeasurement CapacitanceManager { get; set; }

    [NotMapped]

    public ICommunication CommunicationManager { get; set; }

    [NotMapped]

    public IConnection ConnectionManager { get; set; }
    [NotMapped]

    public IContinuityMeasurement ContinuityManager { get; set; }

    [NotMapped]

    public IDcVoltageMeasurement DcVoltageManager { get; set; }

    [NotMapped]

    public IResistanceMeasurement ResistanceManager { get; set; }

    /// <summary>
    /// Метод инициализации быстрого измерителя.
    /// </summary>
    /// <returns>Возвращает true, если инициализация прошла успешно.</returns>
    public async Task<bool> Initialize()
    {
      if (IPAddress.TryParse(ConnectionDetails, out IPAddress address))
      {
        var connect = await NewCore.Communication.DeviceCommandSender.PingAsync("Мультиметр", address);
        return connect;
      }

      throw new NotFiniteNumberException();
    }
  }
}
