using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Usb;
using Ask.Device.Communication.Usb.Protocols;
using Ask.Device.Runtime.Base.Device;

namespace Ask.Device.Runtime.Device
{
  /// <summary>
  /// UPS device model MIK-UPS-1101R-RM.
  /// </summary>
  public class MikUps1101rRmDevice : DeviceWithUSB, IUninterruptiblePowerSupply
  {
    public MikUps1101rRmDevice()
    {
      Name = "MIK-UPS-1101R-RM";
      Description = "Бесперебойник";
      DeviceClass = GetType().FullName ?? string.Empty;
      DeviceType = DeviceType.UninterruptiblePowerSupply;

      ConnectionDetails = "VID_0665&PID_5161";
      ConnectableManager = new Function.MikUps1101rRm.ConnectableManager(this);
      DeviceProtocol = new UsbProtocol(this, new Function.MikUps1101rRm.ViewPowerUpsUsbCommandHandler());
      PowerManager = new Function.MikUps1101rRm.PowerManager(this);
    }

    /// <inheritdoc />
    public int NumberChassis { get; set; }

    /// <inheritdoc />
    public string LastResolvedDevicePath { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт реализацию управления питанием UPS.
    /// </summary>
    public IPower PowerManager { get; set; }

    /// <inheritdoc />
    public Task StopPowerAsync(IUserInteractionService? userMessageService = null)
    {
      return PowerManager.StopPowerAsync(userMessageService);
    }

    /// <inheritdoc />
    public Task StartPowerAsync(IUserInteractionService? userMessageService = null)
    {
      return PowerManager.StartPowerAsync(userMessageService);
    }

    /// <inheritdoc />
    public Task<bool> VerifyPowerAsync(IUserInteractionService? userMessageService = null)
    {
      return PowerManager.VerifyPowerAsync(userMessageService);
    }

    public UninterruptiblePowerSupplyDto Convert()
    {
      return new UninterruptiblePowerSupplyDto
      {
        Id = Id,
        NumberChassis = NumberChassis,
        Name = Name ?? string.Empty,
        Description = Description ?? string.Empty,
        Number = Number,
        ConnectionDetails = ConnectionDetails ?? string.Empty,
        DeviceType = DeviceType,
        DeviceClass = DeviceClass ?? string.Empty,
        LastResolvedDevicePath = LastResolvedDevicePath ?? string.Empty
      };
    }
  }
}
