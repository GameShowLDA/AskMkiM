using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;

namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands
{
  public class CapacitanceMeasurementProfile : IMeasurementProfile
  {
    public MultimeterTypeMode TypeMode => MultimeterTypeMode.Capacitance;
    public ElectricalTestFunction ElectricalTest => ElectricalTestFunction.Capacitance;
    public Enum Unit => CapacitanceUnit.NanoFarad;

    public string SetMode { get; init; } = "CONF:CAP";

    public string CheckMode { get; init; } = "CAP";

    public string GetMode { get; init; } = "FUNC?";

    public string Measure { get; init; } = "MEAS:CAP?";

    /// <summary>
    /// Время ожидания ответа.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
