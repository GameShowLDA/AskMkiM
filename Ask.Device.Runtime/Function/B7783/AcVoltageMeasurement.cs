using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;

namespace Ask.Device.Runtime.Function.B7783
{
  public sealed class AcVoltageMeasurement : VoltageMeasurementBase, IAcVoltageMeasurement
  {
    private static readonly double[] Ranges = [0.1d, 1d, 10d, 100d, 750d];

    public AcVoltageMeasurement(MultimeterB7783 device)
      : base(device, Ranges)
    {
    }

    protected override string FunctionName => "AC";

    protected override string ScpiFunctionName => "VOLT:AC";

    protected override MultimeterTypeMode TargetMode => MultimeterTypeMode.AcVoltage;

    protected override string ModeResponseToken => "VOLT:AC";

    public Task<bool> SetACVoltageModeAsync(IUserInteractionService? userMessageService = null)
    {
      return SetVoltageModeAsync(userMessageService: userMessageService);
    }

    public Task<double> MeasureACVoltageAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      IUserInteractionService? userMessageService = null)
    {
      return MeasureVoltageAsync(param, rangeFrom, rangeTo, userMessageService);
    }
  }
}
