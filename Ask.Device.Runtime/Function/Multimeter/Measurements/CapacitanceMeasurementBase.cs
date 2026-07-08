using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.Multimeter.Measurements.Common;

namespace Ask.Device.Runtime.Function.Multimeter.Measurements
{
  internal class CapacitanceMeasurementBase : ICapacitanceMeasurement
  {
    private readonly IMultimeter _device;

    public CapacitanceMeasurementBase(IMultimeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetCapacitanceModeAsync(IUserInteractionService? userMessageService = null) => await SetModeBase.SetModeAsync(_device, _device.CapacitanceCommands, userMessageService);

    /// <inheritdoc />
    public async Task<double> MeasureCapacitanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
        => await MeasurementBase.MeasureAsync(_device, _device.CapacitanceCommands, param, rangeFrom, rangeTo, userMessageService);
  }
}
