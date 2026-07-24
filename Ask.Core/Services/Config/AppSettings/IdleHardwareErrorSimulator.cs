namespace Ask.Core.Services.Config.AppSettings;

/// <summary>
/// Определяет необходимость имитации аппаратной ошибки в холостом режиме.
/// </summary>
public static class IdleHardwareErrorSimulator
{
  private const int ProbabilityDenominator = 2;
  private const int FailureRoll = 0;

  /// <summary>
  /// Текст ответа для имитированной ошибки выполнения команды оборудования.
  /// </summary>
  public const string ErrorMessage = "Оборудование не выполнило команду в холостом режиме.";

  /// <summary>
  /// Проверяет, должна ли текущая аппаратная операция завершиться имитированной ошибкой.
  /// </summary>
  /// <returns>
  /// <see langword="true"/>, если для текущего вызова выбрана аппаратная ошибка.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool ShouldSimulateHardwareError()
  {
    return ShouldSimulateHardwareError(Random.Shared.Next(ProbabilityDenominator));
  }

  /// <summary>
  /// Проверяет результат попытки с заданным случайным выбором.
  /// </summary>
  /// <param name="roll">Случайное целое число: ноль или один.</param>
  /// <returns>
  /// <see langword="true"/>, если настройки разрешают симуляцию и выбрана аппаратная ошибка.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool ShouldSimulateHardwareError(int roll)
  {
    return ExecutionConfig.GetIsIdleModeEnabled()
      && ExecutionConfig.GetIsHardwareErrorSimulationEnabled()
      && IsFailureRoll(roll);
  }

  /// <summary>
  /// Проверяет результат случайного выбора для вероятности один к двум.
  /// </summary>
  /// <param name="roll">Случайное целое число: ноль или один.</param>
  /// <returns>
  /// <see langword="true"/>, если число соответствует аппаратной ошибке.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool IsFailureRoll(int roll) => roll == FailureRoll;
}
