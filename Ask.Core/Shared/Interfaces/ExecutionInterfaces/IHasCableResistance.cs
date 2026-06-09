namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  /// <summary>
  /// Содержит информацию о сопротивлении проводов.
  /// </summary>
  public interface IHasCableResistance
  {
    /// <summary>
    /// Исходное представление сопротивления проводов
    /// (например, "10 Ом").
    /// </summary>
    string? CabelResistanceSource { get; set; }
  }
}
