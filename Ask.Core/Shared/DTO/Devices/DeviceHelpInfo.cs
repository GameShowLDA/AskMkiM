namespace Ask.Core.Shared.DTO.Devices
{
  public class DeviceHelpInfo
  {
    public string DeviceName { get; init; } = string.Empty;
    public IReadOnlyList<DeviceCommandInfo> Commands { get; init; } = [];
  }
}
