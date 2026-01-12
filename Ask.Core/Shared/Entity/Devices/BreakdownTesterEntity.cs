using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.Entity.Devices
{
  /// <summary>
  /// Класс, представляющий сущность пробойной установки.
  /// </summary>
  public class BreakdownTesterEntity : IBreakdownTester
  {
    [Key]
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
    public string DeviceClass { get; set; }

    /// <inheritdoc />
    public int MaxVoltage { get; set; }

    /// <inheritdoc />
    public int IRMinVoltage { get; set; }

    /// <inheritdoc />
    public DeviceType DeviceType => DeviceType.BreakdownTester;

    /// <inheritdoc />
    [NotMapped]
    public IAcwModeBreakdown AcwManger { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IDcwModeBreakdown DcwManger { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IIrModeBreakdown IrManger { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public ISystemSettingsBreakdown SystemManger { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IConnectable ConnectableManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IDeviceProtocol DeviceProtocol { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public ISelfTestCheckerBreakdownTester SelfTestManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public BreakdownTypeMode Mode { get; set; }
  }
}
