using Ask.Protocol.Messages.EntryPoints;
using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Device.Emulator;
using Ask.Device.Runtime.Function.Helpers;
using System.Globalization;
using System.Text.RegularExpressions;
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
    /// <param name="param">Значение, используемое в режиме имитации.</param>
    /// <param name="rangeFrom">Нижняя граница допустимого диапазона.</param>
    /// <param name="rangeTo">Верхняя граница допустимого диапазона.</param>
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
      double responseDelay = 0)
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
        responseDelay);
    }

    /// <summary>
    /// Выполняет одиночное измерение с использованием указанного профиля.
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
      double responseDelay = 0)
    {
      var header = EnumExtensions.GetDescription(profile.ElectricalTest);
      var unit = profile.Unit.GetUnit();

      if (device.TypeMode != profile.TypeMode)
      {
        await SetModeBase.SetModeAsync(device, profile, userMessageService);
      }

      if (profile.ElectricalTest == ElectricalTestFunction.DCVoltage
      || profile.ElectricalTest == ElectricalTestFunction.ACVoltage
      || profile.ElectricalTest == ElectricalTestFunction.Resistance
      || profile.ElectricalTest == ElectricalTestFunction.Diode
      || profile.ElectricalTest == ElectricalTestFunction.Capacitance)
      {
        await RangeBase.SetRangeForMeasurementAsync(device, measurementRange.TargetValue, userMessageService);
      }

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
        maxAttempts: userMessageService == null ? 2 : 1);

      if (!execution.Success)
      {
        await DeviceMessages.PublishOperationResultAsync(
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

      await ShowMeasurementResultAsync(
        header,
        MeasurementValueFormatter.FormatWithUnit(execution.Value, unit),
        true,
        userMessageService);

      return execution.Value;
    }

    /// <summary>
    /// Выполняет серию измерений сопротивления и применяет правило допустимого
    /// количества ложных результатов.
    /// </summary>
    /// <param name="device">Мультиметр.</param>
    /// <param name="profile">Профиль измерения сопротивления.</param>
    /// <param name="measurementRange">Ожидаемое значение и допустимые границы результата.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="correctMeasurementCount">Обязательное количество результатов в допустимых границах.</param>
    /// <param name="falseMeasurementCount">Допустимое количество результатов вне допустимых границ.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    /// <returns>
    /// Среднее правильных результатов, если серия прошла проверку; иначе один
    /// из результатов вне допустимых границ.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если <paramref name="correctMeasurementCount"/> меньше единицы,
    /// <paramref name="falseMeasurementCount"/> меньше нуля или их сумма превышает
    /// <see cref="int.MaxValue"/>.
    /// </exception>
    static public async Task<double> MeasureResistanceAsync(
      IMultimeter device,
      IMeasurementProfile profile,
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      int correctMeasurementCount = 2,
      int falseMeasurementCount = 1,
      double responseDelay = 0)
    {
      if (correctMeasurementCount < 1)
      {
        throw new ArgumentOutOfRangeException(
          nameof(correctMeasurementCount),
          correctMeasurementCount,
          "Количество правильных измерений должно быть больше нуля.");
      }

      if (falseMeasurementCount < 0)
      {
        throw new ArgumentOutOfRangeException(
          nameof(falseMeasurementCount),
          falseMeasurementCount,
          "Количество ложных измерений не может быть отрицательным.");
      }

      if (correctMeasurementCount > int.MaxValue - falseMeasurementCount)
      {
        throw new ArgumentOutOfRangeException(
          nameof(falseMeasurementCount),
          falseMeasurementCount,
          "Суммарное количество измерений превышает допустимое значение.");
      }

      int totalMeasurementCount = correctMeasurementCount + falseMeasurementCount;

      var header = EnumExtensions.GetDescription(profile.ElectricalTest);
      var unit = profile.Unit.GetUnit();

      if (device.TypeMode != profile.TypeMode)
      {
        await SetModeBase.SetModeAsync(device, profile, userMessageService);
      }

      await RangeBase.SetRangeForMeasurementAsync(device, measurementRange.TargetValue, userMessageService);

      var correctMeasurements = new List<double>(totalMeasurementCount);
      var falseMeasurements = new List<double>(totalMeasurementCount);
      bool showIntermediateResults = DeviceDisplayConfig.GetIntermediateMeasurementResultsVisibility();

      for (int measurementNumber = 1; measurementNumber <= totalMeasurementCount; measurementNumber++)
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
          maxAttempts: userMessageService == null ? 2 : 1);

        if (!execution.Success)
        {
          await DeviceMessages.PublishOperationResultAsync(
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

        double measurement = execution.Value;
        bool isCorrect = IsWithinRange(
          measurement,
          measurementRange.LowerBound,
          measurementRange.UpperBound);

        if (isCorrect)
        {
          correctMeasurements.Add(measurement);
        }
        else
        {
          falseMeasurements.Add(measurement);
        }

        if (showIntermediateResults)
        {
          await DeviceMessages.PublishOperationResultAsync(
            device,
            GetIntermediateMeasurementHeader(profile.ElectricalTest),
            MeasurementValueFormatter.FormatWithUnit(measurement, unit),
            isCorrect,
            2,
            userMessageService,
            isStepCheckpoint: true);
        }
      }

      bool isSuccessful = correctMeasurements.Count >= correctMeasurementCount;
      double result = isSuccessful
        ? correctMeasurements.Average()
        : falseMeasurements[0];

      await ShowMeasurementResultAsync(
        header,
        MeasurementValueFormatter.FormatWithUnit(result, unit),
        isSuccessful,
        userMessageService);

      return result;
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
      var intermediateResultHeader = GetIntermediateMeasurementHeader(profile.ElectricalTest);
      var unit = profile.Unit.GetUnit();

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
        await ShowMeasurementAttemptStepAsync(
          device,
          header,
          attempt,
          maxMeasurementAttempts,
          measurements.Count + 1,
          measurementCount,
          userMessageService);

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
            await DeviceMessages.PublishOperationResultAsync(
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
        bool isWithinRange = IsWithinRange(measurement, measurementRange.LowerBound, measurementRange.UpperBound);

        if (showIntermediateResults)
        {
          await DeviceMessages.PublishOperationResultAsync(
            device,
            intermediateResultHeader,
            MeasurementValueFormatter.FormatWithUnit(measurement, unit),
            isPositive && isWithinRange,
            2,
            userMessageService,
            isStepCheckpoint: true);
        }

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

        await DeviceMessages.PublishOperationResultAsync(
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
      await ShowMeasurementResultAsync(
        header,
        MeasurementValueFormatter.FormatWithUnit(result, unit),
        IsWithinRange(result, measurementRange.LowerBound, measurementRange.UpperBound),
        userMessageService);

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
    /// Возвращает короткое имя операции для вывода промежуточного результата измерения.
    /// </summary>
    static private string GetIntermediateMeasurementHeader(ElectricalTestFunction electricalTest)
    {
      return electricalTest switch
      {
        ElectricalTestFunction.Capacitance => "Измерение ёмкости",
        ElectricalTestFunction.Resistance => "Измерение сопротивления",
        ElectricalTestFunction.ACVoltage or ElectricalTestFunction.DCVoltage => "Измерение напряжения",
        _ => EnumExtensions.GetDescription(electricalTest),
      };
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
      double responseDelay = 0)
    {
      if (!ExecutionConfig.GetIsIdleModeEnabled() && !device.ConnectionInfo.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await ShowMeasurementCommandStepAsync(device, header, profile.Measure, userMessageService);

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
        timeout: profile.Timeout);
      LogInformation($"[{header}] ответ мультиметра: {response}");

      response = response.Trim().Replace("+", "");
      string numericResponse = ExtractNumericValue(response);

      if (double.TryParse(numericResponse, NumberStyles.Float, CultureInfo.InvariantCulture, out double measurementValue))
      {
        if (profile.Unit is CapacitanceUnit)
        {
          return MeasurementAdapterHelper.Round(measurementValue * 1e9);
        }

        return MeasurementAdapterHelper.Round(measurementValue);
      }

      throw new InvalidOperationException(LogError($"Не удалось обработать значение при \"{header}\": {response}", isDeviceLog: true));
    }

    /// <summary>
    /// Извлекает числовое значение из ответа устройства.
    /// </summary>
    /// <param name="response">Строка ответа устройства.</param>
    /// <returns>Строковое представление числового значения.</returns>
    static private string ExtractNumericValue(string response)
    {
      var match = Regex.Match(
        response,
        @"^[+-]?(?:\d+(?:[.,]\d*)?|[.,]\d+)(?:[eE][+-]?\d+)?");

      if (!match.Success)
      {
        return response;
      }

      return match.Value.Replace(',', '.');
    }

    /// <summary>
    /// Выводит итоговый результат измерения с учетом настроек отображения параметров устройства.
    /// </summary>
    static private async Task ShowMeasurementResultAsync(
      string header,
      string message,
      bool result,
      IUserInteractionService? userMessageService)
    {
      if (userMessageService == null || (result && !DeviceDisplayConfig.GetExecutionParametersVisibility()))
      {
        return;
      }

      var resultType = result
        ? ShowMessageModel.MessageType.Success
        : ShowMessageModel.MessageType.Error;
      var resultMessage = !result || DeviceDisplayConfig.GetMeasurementResultsVisibility()
        ? message
        : string.Empty;

      await userMessageService.ShowMessageAsync(
        new ShowMessageModel(
          header: $"Результат \"{header}\"",
          message: resultMessage,
          type: resultType)
        {
          IndentLevel = 2,
          IsStepModeCheckpoint = true,
        },
        IsBlockStart: true,
        skipPause: true);
    }

    /// <summary>
    /// Выводит шаг отправки измерительной команды только при активном пошаговом режиме.
    /// </summary>
    static private Task ShowMeasurementCommandStepAsync(
      IMultimeter device,
      string header,
      string command,
      IUserInteractionService? userMessageService)
    {
      if (userMessageService == null || !StepControlManager.StepMode)
      {
        return Task.CompletedTask;
      }

      return userMessageService.ShowMessageAsync(
        new ShowMessageModel(
          header: $"{device.Name}({device.NumberChassis}.{device.Number}) - Команда измерения \"{header}\"",
          message: command,
          type: ShowMessageModel.MessageType.Command)
        {
          IndentLevel = 2,
          IsDeviceMessage = true,
          IsStepModeCheckpoint = true,
          IsControlProgramCommandHeader = true,
        },
        IsBlockStart: true,
        skipPause: true);
    }

    /// <summary>
    /// Выводит шаг очередной попытки измерения ёмкости только при активном пошаговом режиме.
    /// </summary>
    static private Task ShowMeasurementAttemptStepAsync(
      IMultimeter device,
      string header,
      int attempt,
      int maxAttempts,
      int acceptedMeasurementNumber,
      int requiredMeasurements,
      IUserInteractionService? userMessageService)
    {
      if (userMessageService == null || !StepControlManager.StepMode)
      {
        return Task.CompletedTask;
      }

      return userMessageService.ShowMessageAsync(
        new ShowMessageModel(
          header: $"{device.Name}({device.NumberChassis}.{device.Number}) - Попытка измерения \"{header}\"",
          message: $"{attempt}/{maxAttempts}; принято {acceptedMeasurementNumber - 1}/{requiredMeasurements}",
          type: ShowMessageModel.MessageType.Command)
        {
          IndentLevel = 2,
          IsDeviceMessage = true,
          IsStepModeCheckpoint = true,
          IsControlProgramCommandHeader = true,
        },
        IsBlockStart: true,
        skipPause: true);
    }
  }
}
