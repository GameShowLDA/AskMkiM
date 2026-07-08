namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands.Connected
{
  public class ConnectedBaseProfile
  {
    public string Initialize { get; set; } = "*IDN?";

    public string CheckMode { get; init; }


    /// <summary>
    /// Время ожидания ответа.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
