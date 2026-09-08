using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.DTO.Devices.Breakdown;

/// <summary>
/// Содержит статус и измеренное значение, возвращённые пробойной установкой.
/// </summary>
public sealed record BreakdownMeasurementResponse
{
  /// <summary>
  /// Создаёт пустой результат измерения.
  /// </summary>
  public BreakdownMeasurementResponse() { }

  /// <summary>
  /// Создаёт результат измерения с заданными значениями.
  /// </summary>
  /// <param name="status">Статус измерения.</param>
  /// <param name="value">Измеренное значение.</param>
  /// <param name="unit">Единица измерения.</param>
  public BreakdownMeasurementResponse(
    BreakdownMeasurementStatus status,
    double value,
    string unit)
  {
    Status = status;
    Value = value;
    Unit = unit;
  }

  /// <summary>
  /// Статус измерения.
  /// </summary>
  public BreakdownMeasurementStatus Status { get; set; }

  /// <summary>
  /// Измеренное значение.
  /// </summary>
  public double Value { get; set; }

  /// <summary>
  /// Единица измерения.
  /// </summary>
  public string Unit { get; set; } = string.Empty;
}
