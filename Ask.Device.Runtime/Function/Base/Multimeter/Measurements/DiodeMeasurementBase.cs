using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements
{
  internal class DiodeMeasurementBase : IDiodeMeasurement
  {
    private readonly IMultimeter _device;
    private readonly SetModeBase setModeBase;
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DiodeMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public DiodeMeasurementBase(IMultimeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetDiodeModeAsync(
      IUserInteractionService? userMessageService = null,
      CancellationToken cancellationToken = default) =>
      await SetModeBase.SetModeAsync(
        _device,
        _device.DiodeCommands,
        userMessageService,
        cancellationToken);


    /// <inheritdoc />
    public async Task<double> CheckDiodeAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      double responseDelay = 0,
      CancellationToken cancellationToken = default)
        => await MeasurementBase.MeasureAsync(
          _device,
          _device.DiodeCommands,
          measurementRange,
          userMessageService,
          responseDelay: responseDelay,
          cancellationToken: cancellationToken);
  }
}
