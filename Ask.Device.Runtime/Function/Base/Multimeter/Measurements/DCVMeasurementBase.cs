using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements
{
  /// <summary>
  /// Реализует операции измерения постоянного напряжения мультиметра.
  /// </summary>
  internal class DCVMeasurementBase : IDcVoltageMeasurement
  {
    /// <summary>
    /// Мультиметр, с которым выполняются измерения.
    /// </summary>
    private readonly IMultimeter _device;

    /// <summary>
    /// Инициализирует обработчик измерения постоянного напряжения.
    /// </summary>
    /// <param name="device">Мультиметр.</param>
    public DCVMeasurementBase(IMultimeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetDCVoltageModeAsync(IUserInteractionService? userMessageService = null) => await SetModeBase.SetModeAsync(_device, _device.DCVCommands, userMessageService);

    /// <inheritdoc />
    public async Task<double> MeasureDCVoltageAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null, double responseDelay = 0)
        => await MeasurementBase.MeasureAsync(_device, _device.DCVCommands, param, rangeFrom, rangeTo, userMessageService, responseDelay: responseDelay);

    /// <inheritdoc />
    public async Task<bool> SetDCVoltageRangeAsync(double range, IUserInteractionService? userMessageService = null)
    {
      if (_device.TypeMode != _device.DCVCommands.TypeMode)
      {
        await SetModeBase.SetModeAsync(_device, _device.DCVCommands, userMessageService);
      }

      return await RangeBase.SetRangeAsync(_device, range, userMessageService);
    }
  }
}
