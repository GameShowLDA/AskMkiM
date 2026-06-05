namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  /// <summary>
  /// Содержит информацию о порогах напряжения.
  /// </summary>
  public interface IHasVoltageLimits
  {
    /// <summary>
    /// Исходное представление нижней границы напряжения.
    /// </summary>
    string? LowerLimitVoltageSource { get; set; }

    /// <summary>
    /// Нижняя граница значения напряжения.
    /// </summary>
    double? LowerLimitVoltage { get; set; }

    /// <summary>
    /// Исходное представление верхней границы напряжения.
    /// </summary>
    string? HigherLimitVoltageSource { get; set; }

    /// <summary>
    /// Верхняя граница значения напряжения.
    /// </summary>
    double? HigherLimitVoltage { get; set; }
  }
}
