namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands
{
  public class ResistanceMeasurementParameters
  {
    public string SetMode { get; init; } = "CONF:RES";

    public string GetMode { get; init; } = "FUNC?";

    public string Measure { get; init; } = "MEAS:RES?";

    /// <summary>
    /// Время ожидания ответа.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
