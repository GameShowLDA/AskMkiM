using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Application.Execution
{
  /// <summary>
  /// Результат выполнения измерительной операции адаптера.
  /// </summary>
  /// <typeparam name="T">Тип полезного результата операции.</typeparam>
  internal readonly struct AdapterMeasurementResult<T>
  {
    /// <summary>
    /// Инициализирует новый экземпляр структуры <see cref="AdapterMeasurementResult{T}"/>.
    /// </summary>
    /// <param name="success">Признак успешного завершения операции.</param>
    /// <param name="value">Полученное значение.</param>
    /// <param name="hasValue">Признак наличия значения.</param>
    /// <param name="errorMessage">Текст последней ошибки.</param>
    /// <param name="attemptsUsed">Количество использованных попыток.</param>
    private AdapterMeasurementResult(bool success, T value, bool hasValue, string errorMessage, int attemptsUsed)
    {
      Success = success;
      Value = value;
      HasValue = hasValue;
      ErrorMessage = errorMessage;
      AttemptsUsed = attemptsUsed;
    }

    /// <summary>
    /// Получает признак успешного завершения операции.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Получает значение, полученное в результате выполнения операции.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Получает признак того, что значение было получено.
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// Получает текст ошибки последней неудачной попытки.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Получает количество использованных попыток.
    /// </summary>
    public int AttemptsUsed { get; }

    /// <summary>
    /// Создаёт успешный результат выполнения.
    /// </summary>
    /// <param name="value">Полученное значение.</param>
    /// <param name="attemptsUsed">Количество использованных попыток.</param>
    /// <returns>Успешный результат выполнения операции.</returns>
    public static AdapterMeasurementResult<T> FromSuccess(T value, int attemptsUsed)
    {
      return new AdapterMeasurementResult<T>(true, value, true, string.Empty, attemptsUsed);
    }

    /// <summary>
    /// Создаёт неуспешный результат выполнения.
    /// </summary>
    /// <param name="errorMessage">Текст ошибки.</param>
    /// <param name="attemptsUsed">Количество использованных попыток.</param>
    /// <param name="value">Последнее полученное значение, если оно было.</param>
    /// <param name="hasValue">Признак наличия последнего значения.</param>
    /// <returns>Неуспешный результат выполнения операции.</returns>
    public static AdapterMeasurementResult<T> FromFailure(string errorMessage, int attemptsUsed, T value = default!, bool hasValue = false)
    {
      return new AdapterMeasurementResult<T>(false, value, hasValue, errorMessage, attemptsUsed);
    }
  }

  /// <summary>
  /// Выполняет измерительные операции адаптеров с повторной попыткой и обязательным логированием.
  /// </summary>
  internal static class AdapterMeasurementExecutor
  {
    /// <summary>
    /// Выполняет измерительную операцию с автоматическим повтором при исключении
    /// или при результате, который признан неуспешным.
    /// </summary>
    /// <typeparam name="T">Тип результата измерения.</typeparam>
    /// <param name="device">Устройство, для которого выполняется операция.</param>
    /// <param name="operationName">Читаемое имя операции для логов.</param>
    /// <param name="operation">Операция измерения.</param>
    /// <param name="shouldRetryOnResult">Проверка результата на необходимость повторного измерения.</param>
    /// <param name="maxAttempts">Максимальное количество попыток.</param>
    /// <returns>Результат выполнения измерительной операции.</returns>
    public static async Task<AdapterMeasurementResult<T>> ExecuteAsync<T>(
      IAttachableDevice device,
      string operationName,
      Func<Task<T>> operation,
      Func<T, bool>? shouldRetryOnResult = null,
      int maxAttempts = 2)
    {
      ArgumentNullException.ThrowIfNull(device);
      ArgumentNullException.ThrowIfNull(operation);

      if (maxAttempts < 1)
      {
        throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "Количество попыток должно быть больше нуля.");
      }

      string deviceLabel = $"{device.Name}({device.NumberChassis}.{device.Number})";
      string lastErrorMessage = "Операция завершилась без результата.";
      T lastValue = default!;
      bool hasValue = false;

      for (int attempt = 1; attempt <= maxAttempts; attempt++)
      {
        LogInformation($"[{deviceLabel}] {operationName}: попытка {attempt}/{maxAttempts}.", isDeviceLog: true);

        try
        {
          T value = await operation();
          lastValue = value;
          hasValue = true;

          if (shouldRetryOnResult?.Invoke(value) == true)
          {
            lastErrorMessage = $"Операция \"{operationName}\" вернула неуспешный результат.";

            if (attempt < maxAttempts)
            {
              LogWarning($"[{deviceLabel}] {operationName}: результат попытки {attempt}/{maxAttempts} признан неуспешным. Выполняется повторное измерение.", isDeviceLog: true);
              continue;
            }

            LogWarning($"[{deviceLabel}] {operationName}: исчерпаны попытки, последний результат признан неуспешным.", isDeviceLog: true);
            return AdapterMeasurementResult<T>.FromFailure(lastErrorMessage, attempt, value, hasValue: true);
          }

          LogInformation($"[{deviceLabel}] {operationName}: попытка {attempt}/{maxAttempts} завершилась успешно.", isDeviceLog: true);
          return AdapterMeasurementResult<T>.FromSuccess(value, attempt);
        }
        catch (Exception ex)
        {
          lastErrorMessage = ex.Message;

          if (attempt < maxAttempts)
          {
            LogWarning($"[{deviceLabel}] {operationName}: ошибка на попытке {attempt}/{maxAttempts}: {ex.Message}. Выполняется повторное измерение.", isDeviceLog: true);
            continue;
          }

          LogException($"[{deviceLabel}] {operationName}: ошибка на финальной попытке.", ex, isDeviceLog: true);
        }
      }

      return AdapterMeasurementResult<T>.FromFailure(lastErrorMessage, maxAttempts, lastValue, hasValue);
    }
  }
}
