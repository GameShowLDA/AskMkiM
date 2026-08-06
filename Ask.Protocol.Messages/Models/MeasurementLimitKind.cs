namespace Ask.Protocol.Messages.Models;

/// <summary>
/// Определяет положение допустимого предела относительно измеряемого значения.
/// </summary>
public enum MeasurementLimitKind
{
  /// <summary>
  /// Минимальный допустимый предел.
  /// </summary>
  Minimum,

  /// <summary>
  /// Максимальный допустимый предел.
  /// </summary>
  Maximum,
}
