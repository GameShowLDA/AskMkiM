using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;

namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands
{
  public class DiodeMeasurementProfile : IMeasurementProfile
  {
    public MultimeterTypeMode TypeMode => MultimeterTypeMode.Diode;
    public ElectricalTestFunction ElectricalTest => ElectricalTestFunction.Diode;
    public Enum Unit => VoltageUnit.Volt;

    public string SetMode { get; init; } = "CONF:DIOD";

    public string CheckMode { get; init; } = "DIOD";

    public string GetMode { get; init; } = "FUNC?";

    public string Measure { get; init; } = "MEAS:DIOD?";

    /// <summary>
    /// Время ожидания ответа.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
