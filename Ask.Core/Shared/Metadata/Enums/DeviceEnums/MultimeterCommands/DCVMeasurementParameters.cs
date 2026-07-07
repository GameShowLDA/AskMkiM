namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands
{
  public class DCVMeasurementParameters
  {
    public string SetMode { get; init; } = "CONF:VOLT:DC";

    public string CheckMode { get; init; } = "VOLT:DC";

    public string GetMode { get; init; } = "FUNC?";

    public string Measure { get; init; } = "MEAS:VOLT:DC?";

    /// <summary>
    /// Время ожидания ответа.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
