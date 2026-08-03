using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements
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
    public async Task<bool> CheckContinuityAsync(bool expectedOutcome, IUserInteractionService? userMessageService = null, double responseDelay = 0)
    {
      if (_device.TypeMode != MultimeterTypeMode.Continuity)
      {
        await SetContinuityModeAsync(userMessageService);
      }

      var execution = await AdapterMeasurementExecutor.ExecuteAsync(
        _device,
        "Прозвонка",
        () => CheckContinuityCoreAsync(expectedOutcome, responseDelay: responseDelay),
        value => !value,
        maxAttempts: userMessageService == null ? 2 : 1);

      if (!execution.Success)
      {
        if (execution.HasValue)
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Ошибка при прозвонке",
            "Результат прозвонки не соответствует ожидаемому состоянию.",
            false,
            2,
            userMessageService);

          return execution.Value;
        }

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Ошибка при прозвонке",
          execution.ErrorMessage,
          false,
          2,
          userMessageService);

        if (userMessageService != null)
        {
          throw new DeviceException(
            $"Ошибка при прозвонке для {_device.Name}({_device.NumberChassis}.{_device.Number}): " +
            execution.ErrorMessage);
        }

        return false;
      }

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _device,
        execution.Value ? "Результат прозвонки" : "Ошибка при прозвонке",
        execution.Value
          ? expectedOutcome ? "Цепь замкнута" : "Цепь разомкнута"
          : "Результат прозвонки не соответствует ожидаемому состоянию.",
        execution.Value,
        2,
        userMessageService);

      return execution.Value;
    }

    /// <inheritdoc />
    public async Task<double> CheckContinuityAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      double responseDelay = 0)
        => await MeasurementBase.MeasureAsync(
          _device,
          _device.ContinuityCommands,
          measurementRange,
          userMessageService,
          responseDelay: responseDelay);

    /// <summary>
    /// Проверяет проводимость между измерительными щупами.
    /// </summary>
    /// <returns>
    /// <c>true</c>, если обнаружено соединение (низкое сопротивление), иначе <c>false</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    private async Task<bool> CheckContinuityCoreAsync(bool expectedOutcome, IUserInteractionService? userMessageService = null, double responseDelay = 0)
    {
      if (!ExecutionConfig.GetIsIdleModeEnabled() && !_device.ConnectionInfo.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      bool actualOutcome = !ExecutionConfig.GetIsErrorSimulationEnabled()
        || Random.Shared.Next(2) == 1
        ? expectedOutcome
        : !expectedOutcome;
      string idleResponse = actualOutcome ? "+1.00000000E+00" : "+9.90000000E+37";
      string response = await MultimeterQueryExecutor.QueryAsync(
        _device,
        _device.ContinuityCommands.Measure,
        idleResponse,
        responseDelay: responseDelay,
        timeout: _device.ContinuityCommands.Timeout);
      if (string.IsNullOrWhiteSpace(response))
      {
        throw new InvalidOperationException("Мультиметр вернул пустой ответ.");
      }

      return response != "+9.90000000E+37" == expectedOutcome;
    }
  }
}
