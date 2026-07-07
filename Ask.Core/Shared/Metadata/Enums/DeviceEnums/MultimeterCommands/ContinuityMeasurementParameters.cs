namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands
{
  public class ContinuityMeasurementParameters
  {
    public string SetMode { get; init; } = "CONF:CONT";

    public string CheckMode { get; init; } = "CONT";

    public string GetMode { get; init; } = "FUNC?";

    public string Measure { get; init; } = "MEAS:CONT?";

    /// <summary>
    /// Время ожидания ответа.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
