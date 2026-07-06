using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;

namespace Ask.Device.Runtime.Function.B7783
{
  public sealed class DcVoltageMeasurement : VoltageMeasurementBase, IDcVoltageMeasurement
  {
    private static readonly double[] Ranges = [0.1d, 1d, 10d, 100d, 1000d];

    public DcVoltageMeasurement(MultimeterB7783 device)
      : base(device, Ranges)
    {
    }

    protected override string FunctionName => "DC";

    protected override string ScpiFunctionName => "VOLT:DC";

    protected override MultimeterTypeMode TargetMode => MultimeterTypeMode.DcVoltage;

    protected override string ModeResponseToken => "VOLT";

    public Task<bool> SetDCVoltageModeAsync(IUserInteractionService? userMessageService = null)
    {
      return SetVoltageModeAsync(userMessageService: userMessageService);
    }

    public Task<double> MeasureDCVoltageAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      IUserInteractionService? userMessageService = null)
    {
      return MeasureVoltageAsync(param, rangeFrom, rangeTo, userMessageService);
    }
  }
}
