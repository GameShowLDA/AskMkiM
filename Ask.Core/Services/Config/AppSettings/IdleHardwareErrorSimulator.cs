using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.Core.Services.Config.AppSettings;

/// <summary>
/// Определяет необходимость имитации аппаратной ошибки в холостом режиме.
/// </summary>
public static class IdleHardwareErrorSimulator
{
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
  public static bool ShouldSimulateHardwareError(IDevice device)
  {
    ArgumentNullException.ThrowIfNull(device);

    return ExecutionConfig.GetIsIdleModeEnabled()
      && device.IsHardwareFailureSimulationEnabled;
  }
}
