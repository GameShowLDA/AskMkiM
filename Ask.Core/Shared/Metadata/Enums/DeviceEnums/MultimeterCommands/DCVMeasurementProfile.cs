using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;

namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands
{
  public class DCVMeasurementProfile : IMeasurementProfile
  {
    public MultimeterTypeMode TypeMode => MultimeterTypeMode.DcVoltage;
    public ElectricalTestFunction ElectricalTest => ElectricalTestFunction.DCVoltage;

    public Enum Unit => VoltageUnit.Volt;


    public string SetMode { get; init; } = "CONF:VOLT:DC";

    public string CheckMode { get; init; } = "VOLT";

    public string GetMode { get; init; } = "FUNC?";

    public string Measure { get; init; } = "MEAS:VOLT:DC?";

    /// <summary>
    /// Время ожидания ответа.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
