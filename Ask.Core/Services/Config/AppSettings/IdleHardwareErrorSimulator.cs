namespace Ask.Core.Services.Config.AppSettings;

using Ask.Core.Shared.Interfaces.DeviceInterfaces;

/// <summary>
/// Определяет необходимость имитации аппаратной ошибки в холостом режиме.
/// </summary>
public static class IdleHardwareErrorSimulator
{
  /// <summary>
  /// Текст ответа для имитированной ошибки выполнения команды оборудования.
  /// </summary>
  public const string ErrorMessage = "Симуляция сбоя: оборудование не выполнило команду в холостом режиме.";

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
    return ShouldSimulateHardwareError(device.IsHardwareFailureSimulationEnabled);
  }

  /// <summary>
  /// Проверяет настройку отдельного устройства с учётом текущего режима выполнения.
  /// </summary>
  /// <param name="isEnabledForDevice">Включена ли симуляция для выбранного устройства.</param>
  /// <returns>
  /// <see langword="true"/>, если настройки разрешают симуляцию и выбрана аппаратная ошибка.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool ShouldSimulateHardwareError(bool isEnabledForDevice)
  {
    return ExecutionConfig.GetIsIdleModeEnabled()
      && isEnabledForDevice;
  }
}
