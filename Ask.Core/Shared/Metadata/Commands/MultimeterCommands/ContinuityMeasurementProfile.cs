using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;

namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands
{
  public class ContinuityMeasurementProfile : IMeasurementProfile
  {
    public MultimeterTypeMode TypeMode => MultimeterTypeMode.Continuity;
    public ElectricalTestFunction ElectricalTest => ElectricalTestFunction.Continuity;
    public Enum Unit => ResistanceUnit.Ohm;

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
