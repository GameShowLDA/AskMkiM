using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Function.Helpers;
using System.Globalization;

namespace Ask.Device.Runtime.Function.Multimeter.Measurements
{
  internal class ContinuityMeasurementBase : IContinuityMeasurement
  {
    /// <summary>
    /// Экземпляр прибора Keysight.
    /// </summary>
    private readonly IFastMeter _device;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ContinuityMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public ContinuityMeasurementBase(IFastMeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetContinuityModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await SetContinuityModeCoreAsync();

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима прозвонки", string.Empty, succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw ContinuityExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      _device.TypeMode = MultimeterTypeMode.Continuity;

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> CheckContinuityAsync(bool expectedOutcome, IUserInteractionService? userMessageService = null)
    {
      if (_device.TypeMode != MultimeterTypeMode.Continuity)
      {
        await SetContinuityModeCoreAsync(userMessageService);
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

    public async Task<double> CheckContinuityAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (_device.TypeMode != MultimeterTypeMode.Continuity)
      {
        await SetContinuityModeCoreAsync(userMessageService);
      }

      if (rangeTo == -1)
      {
        rangeTo = double.MaxValue;
      }

      var random = Simulated.GetSimulatedValue(rangeFrom, rangeTo, ElectricalTestFunction.Continuity);
      if (random != -1)
      {
        return random;
      }

      var execution = await AdapterMeasurementExecutor.ExecuteAsync(
        _device,
        "Измерение прозвонки",
        () => CheckContinuityCoreAsync(param, rangeFrom, rangeTo));

      if (!execution.Success)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при прозвонке", execution.ErrorMessage, false, 2, userMessageService);
        return -1;
      }

      double result = execution.Value;
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат прозвонки", result.ToString(), true, 2, userMessageService);
      return result;
    }

    /// <summary>
    /// Устанавливает прибор в режим прозвонки (Continuity Test).
    /// </summary>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    public async Task<bool> SetContinuityModeCoreAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }
      if (_device.TypeMode == MultimeterTypeMode.Continuity)
      {
        return true;
      }

      await _device.DeviceProtocol.QueryAsync(_device.ContinuityCommands.SetMode);
      var answer = await _device.DeviceProtocol.QueryAsync(_device.ContinuityCommands.GetMode, timeout: _device.ContinuityCommands.Timeout);
      if (answer.Contains(_device.ContinuityCommands.CheckMode))
      {
        _device.TypeMode = MultimeterTypeMode.Continuity;
        return true;
      }

      return false;
    }

    /// <summary>
    /// Проверяет проводимость между измерительными щупами.
    /// </summary>
    /// <returns>
    /// <c>true</c>, если обнаружено соединение (низкое сопротивление), иначе <c>false</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    public async Task<bool> CheckContinuityCoreAsync(bool expectedOutcome, IUserInteractionService? userMessageService = null)
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync(_device.ContinuityCommands.Measure, timeout: _device.ContinuityCommands.Timeout);
      return response != "+9.90000000E+37" == expectedOutcome;
    }

    /// <summary>
    /// Проверяет проводимость между измерительными щупами.
    /// </summary>
    /// <returns>
    /// <c>true</c>, если обнаружено соединение (низкое сопротивление), иначе <c>false</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    public async Task<double> CheckContinuityCoreAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync(_device.ContinuityCommands.Measure, timeout: _device.ContinuityCommands.Timeout);
      if (response.Contains("+9.90000000E+37"))
        return 1001;

      if (double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
      {
        return MeasurementAdapterHelper.Round(value);
      }

      throw new FormatException($"Неверный формат ответа прибора: '{response}'");
    }
  }
}
