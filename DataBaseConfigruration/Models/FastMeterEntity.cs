using System.ComponentModel.DataAnnotations.Schema;
using NewCore.Base.Device;
using NewCore.Base.Function.FastMeter;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Enum;

namespace DataBaseConfiguration.Models
{
  /// <summary>
  /// Класс, представляющий сущность быстрого измерителя.
  /// </summary>
  public class FastMeterEntity : IFastMeter
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
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.FastMeter;

    /// <inheritdoc />
    public string DeviceClass { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IAcVoltageMeasurement AcVoltageManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public ICapacitanceMeasurement CapacitanceManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IConnection ConnectionManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IContinuityMeasurement ContinuityManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IDcVoltageMeasurement DcVoltageManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IResistanceMeasurement ResistanceManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IConnectable ConnectableManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IDeviceProtocol DeviceProtocol { get; set; }
  }
}
