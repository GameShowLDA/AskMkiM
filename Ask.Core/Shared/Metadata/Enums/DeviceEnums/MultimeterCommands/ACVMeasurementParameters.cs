namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands
{
  public class ACVMeasurementParameters
  {
    public string SetMode { get; init; } = "CONF:VOLT:AC";

    public string CheckMode { get; init; } = "VOLT:AC";

    public string GetMode { get; init; } = "FUNC?";

    public string Measure { get; init; } = "MEAS:VOLT:AC?";

    /// <summary>
    /// Время ожидания ответа.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
