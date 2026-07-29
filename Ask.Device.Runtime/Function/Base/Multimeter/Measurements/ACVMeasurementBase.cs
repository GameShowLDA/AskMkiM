using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements
{
  /// <summary>
  /// Реализует операции измерения переменного напряжения мультиметра.
  /// </summary>
  internal class ACVMeasurementBase : IAcVoltageMeasurement
  {
    /// <summary>
    /// Мультиметр, с которым выполняются измерения.
    /// </summary>
    private readonly IMultimeter _device;

    /// <summary>
    /// Инициализирует обработчик измерения переменного напряжения.
    /// </summary>
    /// <param name="device">Мультиметр.</param>
    public ACVMeasurementBase(IMultimeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetACVoltageModeAsync(IUserInteractionService? userMessageService = null) => await SetModeBase.SetModeAsync(_device, _device.ACVCommands, userMessageService);

    /// <inheritdoc />
    public async Task<double> MeasureACVoltageAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      double responseDelay = 0)
        => await MeasurementBase.MeasureAsync(
          _device,
          _device.ACVCommands,
          measurementRange,
          userMessageService,
          responseDelay: responseDelay);

    public async Task<bool> SetACVoltageRangeAsync(double range, IUserInteractionService? userMessageService = null)
    {
      if (_device.TypeMode != _device.ACVCommands.TypeMode)
      {
        await SetModeBase.SetModeAsync(_device, _device.ACVCommands, userMessageService);
      }

      return await RangeBase.SetRangeAsync(_device, range, userMessageService);
    }
  }
}
