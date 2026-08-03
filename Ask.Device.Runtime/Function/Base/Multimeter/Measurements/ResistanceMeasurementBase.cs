using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements
{
  /// <summary>
  /// Реализует операции измерения электрического сопротивления мультиметра.
  /// </summary>
  internal class ResistanceMeasurementBase : IResistanceMeasurement
  {
    private const int DefaultCorrectMeasurementCount = 2;
    private const int DefaultFalseMeasurementCount = 1;

    /// <summary>
    /// Мультиметр, с которым выполняются измерения.
    /// </summary>
    private readonly IMultimeter _device;

    /// <summary>
    /// Инициализирует обработчик измерения электрического сопротивления.
    /// </summary>
    /// <param name="device">Мультиметр.</param>
    public ResistanceMeasurementBase(IMultimeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<double> MeasureResistanceAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      double responseDelay = 0)
        => await MeasureResistanceAsync(
          measurementRange,
          userMessageService,
          DefaultCorrectMeasurementCount,
          DefaultFalseMeasurementCount,
          responseDelay);

    /// <inheritdoc />
    public async Task<double> MeasureResistanceAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService,
      int correctMeasurementCount,
      int falseMeasurementCount,
      double responseDelay = 0)
        => await MeasurementBase.MeasureResistanceAsync(
          _device,
          _device.ResistanceCommands,
          measurementRange,
          userMessageService,
          correctMeasurementCount,
          falseMeasurementCount,
          responseDelay);

    /// <inheritdoc />
    public async Task<bool> SetResistanceModeAsync(IUserInteractionService? userMessageService = null) => await SetModeBase.SetModeAsync(_device, _device.ResistanceCommands, userMessageService);

    /// <inheritdoc />
    public async Task<bool> SetResistanceRangeAsync(double range, IUserInteractionService? userMessageService = null)
    {
      if (_device.TypeMode != _device.ResistanceCommands.TypeMode)
      {
        await SetModeBase.SetModeAsync(_device, _device.ResistanceCommands, userMessageService);
      }

      return await RangeBase.SetRangeAsync(_device, range, userMessageService);
    }
  }
}
