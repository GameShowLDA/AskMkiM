namespace Ask.Core.Services.Validation.Devices;

/// <summary>
/// Проверяет параметры конфигурации модуля коммутации реле.
/// </summary>
public sealed class RelaySwitchModuleConfigurationValidator : IRelaySwitchModuleConfigurationValidator
{
  /// <summary>
  /// Минимальное допустимое количество точек.
  /// </summary>
  public const int MinimumPointCount = 1;

  /// <summary>
  /// Максимальное допустимое количество точек.
  /// </summary>
  public const int MaximumPointCount = 4096;

  /// <inheritdoc />
  public bool TryValidatePointCount(int pointCount, out string errorMessage)
  {
    if (pointCount is >= MinimumPointCount and <= MaximumPointCount)
    {
      errorMessage = string.Empty;
      return true;
    }

    errorMessage = $"Количество точек должно быть от {MinimumPointCount} до {MaximumPointCount}.";
    return false;
  }
}
