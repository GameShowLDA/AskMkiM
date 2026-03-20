using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.Entity.Devices
{
  /// <summary>
  /// Database entity for UPS devices.
  /// </summary>
  public class UninterruptiblePowerSupplyEntity : IUninterruptiblePowerSupply
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
    public DeviceType DeviceType => DeviceType.UninterruptiblePowerSupply;

    /// <inheritdoc />
    public string DeviceClass { get; set; }

    /// <inheritdoc />
    public string LastResolvedDevicePath { get; set; } = string.Empty;

    /// <inheritdoc />
    [NotMapped]
    public IConnectable ConnectableManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IDeviceProtocol DeviceProtocol { get; set; }

    /// <inheritdoc />
    public Task StopPowerAsync(IUserInteractionService? userMessageService = null)
    {
      return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartPowerAsync(IUserInteractionService? userMessageService = null)
    {
      return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> VerifyPowerAsync(IUserInteractionService? userMessageService = null)
    {
      return Task.FromResult(true);
    }
  }
}
