using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Usb;
using NewCore.Base.Device;
using NewCore.FunctionAdapters.MikUps1101rRm;

namespace NewCore.Device
{
  /// <summary>
  /// UPS device model MIK-UPS-1101R-RM.
  /// </summary>
  public class MikUps1101rRmDevice : DeviceWithUSB, IUninterruptiblePowerSupply
  {
    private readonly IPower _powerManager;

    public MikUps1101rRmDevice()
    {
      Name = "MIK-UPS-1101R-RM";
      Description = "Бесперебойник";
      DeviceClass = GetType().FullName ?? string.Empty;
      DeviceType = DeviceType.UninterruptiblePowerSupply;

      ConnectionDetails = "VID_0665&PID_5161";
      ConnectableManager = new ConnectableManagerAdapter(this);
      DeviceProtocol = new UsbProtocol(this, new Function.MikUps1101rRm.ViewPowerUpsUsbCommandHandler());

      _powerManager = new PowerManagerAdapter(this);
    }

    /// <inheritdoc />
    public int NumberChassis { get; set; }

    /// <inheritdoc />
    public string LastResolvedDevicePath { get; set; } = string.Empty;

    /// <inheritdoc />
    public Task StopPowerAsync(IUserInteractionService? userMessageService = null)
    {
      return _powerManager.StopPowerAsync(userMessageService);
    }

    /// <inheritdoc />
    public Task StartPowerAsync(IUserInteractionService? userMessageService = null)
    {
      return _powerManager.StartPowerAsync(userMessageService);
    }

    /// <inheritdoc />
    public Task<bool> VerifyPowerAsync(IUserInteractionService? userMessageService = null)
    {
      return _powerManager.VerifyPowerAsync(userMessageService);
    }
  }
}
