using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;

namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands
{
  public class ACVMeasurementProfile : IMeasurementProfile
  {
    public MultimeterTypeMode TypeMode  => MultimeterTypeMode.AcVoltage;
    public ElectricalTestFunction ElectricalTest => ElectricalTestFunction.ACVoltage;
    public Enum Unit => VoltageUnit.Volt;

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
