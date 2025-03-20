using NewCore.Base.Interface.Main;
using NewCore.Enum;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность точного измерителя.
  /// </summary>
  public class PrecisionMeterEntity : IPrecisionMeter
  {
    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public int NumberChassis { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public int Number { get; set; }

    /// <inheritdoc />
    public string ConnectionDetails { get; set; }

    /// <inheritdoc />
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.PrecisionMeter;

    /// <inheritdoc />
    public string DeviceClass { get; set; }
  }
}
