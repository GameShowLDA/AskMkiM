using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Device.Emulator;
using Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing;
using Ask.Device.Runtime.Function.Helpers;
using System.Globalization;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common
{
  /// <summary>
  /// Предоставляет базовые методы выполнения измерений мультиметром.
  /// </summary>
  static internal class MeasurementBase
  {
    /// <summary>
    /// Выполняет измерение в соответствии с указанным профилем.
    /// </summary>
    /// <param name="device">Мультиметр.</param>
    /// <param name="profile">Профиль измерения.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="measurementCount">Количество положительных результатов измерения ёмкости для усреднения.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    /// <returns>Измеренное значение.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если для измерения ёмкости <paramref name="measurementCount"/> меньше единицы.
    /// </exception>
    static public async Task<double> MeasureAsync(
      IMultimeter device,
      IMeasurementProfile profile,
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      int measurementCount = 1,
      double responseDelay = 0,
      CancellationToken cancellationToken = default)
    {
      if (profile.Unit is CapacitanceUnit)
      {
        return await MeasureCapacitanceAsync(
          device,
          profile,
          measurementRange,
          userMessageService,
          measurementCount,
          responseDelay);
      }

      return await MeasureOtherAsync(
        device,
        profile,
        measurementRange,
        userMessageService,
        responseDelay,
        cancellationToken);
    }

    /// <summary>
    /// Выполняет измерение с повтором при выходе первого результата за допустимый диапазон.
    /// </summary>
    /// <param name="device">Мультиметр.</param>
    /// <param name="profile">Профиль измерения.</param>
    /// <param name="param">Значение, используемое в режиме имитации.</param>
    /// <param name="rangeFrom">Нижняя граница допустимого диапазона.</param>
    /// <param name="rangeTo">Верхняя граница допустимого диапазона.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    /// <returns>Измеренное значение.</returns>
    static private async Task<double> MeasureOtherAsync(
      IMultimeter device,
      IMeasurementProfile profile,
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      double responseDelay = 0,
      CancellationToken cancellationToken = default)
    {
      var header = EnumExtensions.GetDescription(profile.ElectricalTest);

      if (device.TypeMode != profile.TypeMode)
      {
        await SetModeBase.SetModeAsync(device, profile, userMessageService, cancellationToken);
      }

      if (profile.ElectricalTest == ElectricalTestFunction.DCVoltage
      || profile.ElectricalTest == ElectricalTestFunction.ACVoltage
      || profile.ElectricalTest == ElectricalTestFunction.Resistance
      || profile.ElectricalTest == ElectricalTestFunction.Diode
      || profile.ElectricalTest == ElectricalTestFunction.Capacitance)
      {
        await RangeBase.SetRangeForMeasurementAsync(device, measurementRange.TargetValue, userMessageService);
      }

      for (int measurementAttempt = 1; measurementAttempt <= 2; measurementAttempt++)
      {
        var execution = await AdapterMeasurementExecutor.ExecuteAsync(
          device,
          header,
          () => MeasureCoreAsync(
            device,
            profile,
            header,
            measurementRange.TargetValue,
            measurementRange.LowerBound,
            measurementRange.UpperBound,
            userMessageService: userMessageService,
            responseDelay: responseDelay,
            cancellationToken: cancellationToken),
          maxAttempts: userMessageService == null ? 2 : 1);

        if (!execution.Success)
        {
          await MultimeterMessages.PublishOperationResultAsync(
            device,
            $"Ошибка при \"{header}\"",
            execution.ErrorMessage,
            false,
            2,
            userMessageService,
            isStepCheckpoint: true);

          if (userMessageService != null)
          {
            throw new DeviceException(
              $"Ошибка при \"{header}\" для {device.Name}({device.NumberChassis}.{device.Number}): " +
              execution.ErrorMessage);
          }

          return -1;
        }

        if (measurementAttempt == 2
        || IsWithinRange(
          execution.Value,
          measurementRange.LowerBound,
          measurementRange.UpperBound))
        {
          return execution.Value;
        }
      }

      throw new InvalidOperationException("Не удалось получить результат измерения.");
    }

    /// <summary>
    /// Выполняет серию измерений ёмкости и усредняет положительные результаты.
    /// </summary>
    /// <param name="device">Мультиметр.</param>
    /// <param name="profile">Профиль измерения.</param>
    /// <param name="param">Значение, используемое в режиме имитации.</param>
    /// <param name="rangeFrom">Нижняя граница допустимого диапазона.</param>
    /// <param name="rangeTo">Верхняя граница допустимого диапазона.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="measurementCount">Количество положительных результатов для усреднения.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    /// <returns>Измеренное значение.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если <paramref name="measurementCount"/> меньше единицы.
    /// </exception>
    static private async Task<double> MeasureCapacitanceAsync(
      IMultimeter device,
      IMeasurementProfile profile,
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      int measurementCount = 1,
      double responseDelay = 0)
    {
      if (measurementCount < 1)
      {
        throw new ArgumentOutOfRangeException(
          nameof(measurementCount),
          measurementCount,
          "Количество измерений должно быть больше нуля.");
      }

      var header = EnumExtensions.GetDescription(profile.ElectricalTest);

      if (device.TypeMode != profile.TypeMode)
      {
        await SetModeBase.SetModeAsync(device, profile, userMessageService);
      }

      await RangeBase.SetRangeForMeasurementAsync(device, measurementRange.TargetValue, userMessageService);

      var measurements = new List<double>(measurementCount);
      int maxMeasurementAttempts = measurementCount + 5;
      string lastErrorMessage = string.Empty;
      bool showIntermediateResults = DeviceDisplayConfig.GetIntermediateMeasurementResultsVisibility();

      for (int attempt = 1; attempt <= maxMeasurementAttempts && measurements.Count < measurementCount; attempt++)
      {
        var execution = await AdapterMeasurementExecutor.ExecuteAsync(
          device,
          header,
          () => MeasureCoreAsync(
            device,
            profile,
            header,
            measurementRange.TargetValue,
            measurementRange.LowerBound,
            measurementRange.UpperBound,
            userMessageService: userMessageService,
            responseDelay: responseDelay),
          maxAttempts: 1);

        if (!execution.Success)
        {
          lastErrorMessage = execution.ErrorMessage;

          if (showIntermediateResults)
          {
            await MultimeterMessages.PublishOperationResultAsync(
              device,
              $"Ошибка при \"{header}\"",
              $"{execution.ErrorMessage}",
              false,
              2,
              userMessageService,
              isStepCheckpoint: true);
          }

          if (userMessageService != null)
          {
            throw new DeviceException(
              $"Ошибка при \"{header}\" для {device.Name}({device.NumberChassis}.{device.Number}): " +
              execution.ErrorMessage);
          }

          continue;
        }

        double measurement = execution.Value;
        bool isPositive = measurement > 0;

        if (isPositive)
        {
          measurements.Add(measurement);
        }
      }

      if (measurements.Count == 0)
      {
        string errorMessage = string.IsNullOrWhiteSpace(lastErrorMessage)
          ? $"Не получено положительных результатов за {maxMeasurementAttempts} попыток."
          : lastErrorMessage;

        await MultimeterMessages.PublishOperationResultAsync(
          device,
          $"Ошибка при \"{header}\"",
          errorMessage,
          false,
          2,
          userMessageService,
          isStepCheckpoint: true);

        return -1;
      }

      double result = measurements.Average();

      return result;
    }

    /// <summary>
    /// Определяет, входит ли результат измерения в допустимый диапазон.
    /// </summary>
    /// <param name="value">Результат измерения.</param>
    /// <param name="rangeFrom">
    /// Нижняя граница допустимого диапазона.
    /// Значение <c>-1</c> означает отсутствие нижней границы.
    /// </param>
    /// <param name="rangeTo">
    /// Верхняя граница допустимого диапазона.
    /// Значение <c>-1</c> означает отсутствие верхней границы.
    /// </param>
    /// <returns>
    /// <see langword="true"/>, если результат входит в допустимый диапазон.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    static private bool IsWithinRange(double value, double rangeFrom, double rangeTo)
    {
      bool isLowerValid = rangeFrom == -1 || value >= rangeFrom;
      bool isUpperValid = rangeTo == -1 || value <= rangeTo;

      return isLowerValid && isUpperValid;
    }

    /// <summary>
    /// Выполняет непосредственное измерение и преобразование ответа устройства.
    /// </summary>
    /// <param name="device">Мультиметр.</param>
    /// <param name="profile">Профиль измерения.</param>
    /// <param name="header">Наименование выполняемой операции.</param>
    /// <param name="param">Значение, используемое в режиме имитации.</param>
    /// <param name="rangeFrom">Нижняя граница допустимого диапазона.</param>
    /// <param name="rangeTo">Верхняя граница допустимого диапазона.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    /// <returns>Измеренное значение.</returns>
    static private async Task<double> MeasureCoreAsync(
      IMultimeter device,
      IMeasurementProfile profile,
      string header,
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      IUserInteractionService? userMessageService = null,
      double responseDelay = 0,
      CancellationToken cancellationToken = default)
    {
      if (!ExecutionConfig.GetIsIdleModeEnabled() && !device.ConnectionInfo.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      double simulatedValue = Simulated.GetSimulatedValue(rangeFrom, rangeTo, profile.ElectricalTest);
      if (profile.Unit is CapacitanceUnit && simulatedValue != -1)
      {
        simulatedValue /= 1e9;
      }

      string idleResponse = simulatedValue == -1
        ? string.Empty
        : simulatedValue.ToString("+0.00000000E+00;-0.00000000E+00", CultureInfo.InvariantCulture);
      string response = await DeviceProtocolEmulator.QueryMultimeterAsync(
        device,
        profile.Measure,
        idleResponse,
        responseDelay: responseDelay,
        timeout: profile.Timeout,
        cancellationToken: cancellationToken);
      LogInformation($"[{header}] ответ мультиметра: {response}");

      if (MultimeterResponseProcessor.TryParseMeasurement(response, out var measurement))
      {
        if (profile.Unit is CapacitanceUnit)
        {
          return MeasurementAdapterHelper.Round(measurement!.Value * 1e9);
        }

        return MeasurementAdapterHelper.Round(measurement!.Value);
      }

      throw new InvalidOperationException(LogError($"Не удалось обработать значение при \"{header}\": {response}", isDeviceLog: true));
    }

  }
}
