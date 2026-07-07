using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;

namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands
{
  public class ResistanceMeasurementProfile : IMeasurementProfile
  {
    public MultimeterTypeMode TypeMode => MultimeterTypeMode.Resistance;
    public ElectricalTestFunction ElectricalTest => ElectricalTestFunction.Resistance;
    public Enum Unit => ResistanceUnit.Ohm;

    public string SetMode { get; init; } = "CONF:RES";

    public string CheckMode { get; init; } = "RES";

    public string GetMode { get; init; } = "FUNC?";

    public string Measure { get; init; } = "MEAS:RES?";

    /// <summary>
    /// Время ожидания ответа.
    /// </summary>
    public int Timeout { get; init; } = 1000;

  }
}
