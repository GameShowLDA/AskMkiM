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
    public async Task<double> MeasureACVoltageAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
        => await MeasurementBase.MeasureAsync(_device, _device.ACVCommands, param, rangeFrom, rangeTo, userMessageService);
  }
}
