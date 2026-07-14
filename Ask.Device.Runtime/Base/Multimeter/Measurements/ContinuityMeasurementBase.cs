using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.Helpers;
using Ask.Device.Runtime.Base.Multimeter.Measurements.Common;

namespace Ask.Device.Runtime.Base.Multimeter.Measurements
{
  internal class ContinuityMeasurementBase : IContinuityMeasurement
  {
    /// <summary>
    /// Экземпляр прибора Keysight.
    /// </summary>
    private readonly IMultimeter _device;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ContinuityMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public ContinuityMeasurementBase(IMultimeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetContinuityModeAsync(IUserInteractionService? userMessageService = null) => await SetModeBase.SetModeAsync(_device, _device.ContinuityCommands, userMessageService);

    /// <inheritdoc />
    public async Task<bool> CheckContinuityAsync(bool expectedOutcome, IUserInteractionService? userMessageService = null)
    {
      if (_device.TypeMode != MultimeterTypeMode.Continuity)
      {
        await SetContinuityModeAsync(userMessageService);
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      var execution = await AdapterMeasurementExecutor.ExecuteAsync(
        _device,
        "Прозвонка",
        () => CheckContinuityCoreAsync(expectedOutcome),
        value => !value);

      if (!execution.Success)
      {
        string errorMessage = string.IsNullOrWhiteSpace(execution.ErrorMessage)
          ? "Результат прозвонки не соответствует ожидаемому состоянию."
          : execution.ErrorMessage;

        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при прозвонке", errorMessage, false, 2, userMessageService);
        return false;
      }

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _device,
        "Результат прозвонки",
        expectedOutcome ? "Цепь замкнута" : "Цепь разомкнута",
        true,
        2,
        userMessageService);

      return execution.Value;
    }

    /// <inheritdoc />
    public async Task<double> CheckContinuityAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
        => await MeasurementBase.MeasureAsync(_device, _device.ContinuityCommands, param, rangeFrom, rangeTo, userMessageService);

    /// <summary>
    /// Проверяет проводимость между измерительными щупами.
    /// </summary>
    /// <returns>
    /// <c>true</c>, если обнаружено соединение (низкое сопротивление), иначе <c>false</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    private async Task<bool> CheckContinuityCoreAsync(bool expectedOutcome, IUserInteractionService? userMessageService = null)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync(_device.ContinuityCommands.Measure, timeout: _device.ContinuityCommands.Timeout);
      return response != "+9.90000000E+37" == expectedOutcome;
    }
  }
}
