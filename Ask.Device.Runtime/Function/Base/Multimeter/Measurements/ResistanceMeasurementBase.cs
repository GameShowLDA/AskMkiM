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
    public async Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
        => await MeasurementBase.MeasureAsync(_device, _device.ResistanceCommands, param, rangeFrom, rangeTo, userMessageService);

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
