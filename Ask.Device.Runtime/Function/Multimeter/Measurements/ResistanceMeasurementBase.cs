using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.Multimeter.Measurements.Common;

namespace Ask.Device.Runtime.Function.Multimeter.Measurements
{
  internal class ResistanceMeasurementBase : IResistanceMeasurement
  {

    private readonly IFastMeter _device;

    public ResistanceMeasurementBase(IFastMeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public async Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
        => await MeasurementBase.MeasureAsync(_device, _device.ResistanceCommands, param, rangeFrom, rangeTo, userMessageService);

    public async Task<bool> SetResistanceModeAsync(IUserInteractionService? userMessageService = null) => await SetModeBase.SetModeAsync(_device, _device.ResistanceCommands, userMessageService);

  }
}
