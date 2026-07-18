namespace Ask.Core.Services.Validation.Devices;

/// <summary>
/// Проверяет параметры конфигурации модуля коммутации реле.
/// </summary>
public interface IRelaySwitchModuleConfigurationValidator
{
  /// <summary>
  /// Проверяет допустимость количества точек модуля коммутации реле.
  /// </summary>
  /// <param name="pointCount">Количество точек.</param>
  /// <param name="errorMessage">Сообщение об ошибке проверки.</param>
  /// <returns>
  /// <see langword="true"/>, если количество точек допустимо.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  bool TryValidatePointCount(int pointCount, out string errorMessage);
}
