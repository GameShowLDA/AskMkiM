using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Base.Multimeter.Measurements.Common;

namespace Ask.Device.Runtime.Base.Multimeter.Measurements
{
  /// <summary>
  /// Реализует операции измерения ёмкости мультиметра.
  /// </summary>
  internal class CapacitanceMeasurementBase : ICapacitanceMeasurement
  {

    /// <summary>
    /// Мультиметр, с которым выполняются измерения.
    /// </summary>
    private readonly IMultimeter _device;

    /// <summary>
    /// Инициализирует обработчик измерения ёмкости.
    /// </summary>
    /// <param name="device">Мультиметр.</param>
    public CapacitanceMeasurementBase(IMultimeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetCapacitanceModeAsync(IUserInteractionService? userMessageService = null) => await SetModeBase.SetModeAsync(_device, _device.CapacitanceCommands, userMessageService);

    /// <inheritdoc />
    public async Task<bool> SetCapacitanceRangeAsync(double range, IUserInteractionService? userMessageService = null)
    {
      if (_device.TypeMode != _device.CapacitanceCommands.TypeMode)
      {
        await SetModeBase.SetModeAsync(_device, _device.CapacitanceCommands, userMessageService);
      }

      return await RangeBase.SetRangeAsync(_device, range, userMessageService);
    }

    /// <inheritdoc />
    public async Task<double> MeasureCapacitanceAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      int measurementCount = 5,
      double responseDelay = 0)
        => await MeasurementBase.MeasureAsync(
          _device,
          _device.CapacitanceCommands,
          measurementRange,
          userMessageService,
          measurementCount,
          responseDelay);
  }
}
