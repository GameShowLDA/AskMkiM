namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  /// <summary>
  /// Содержит информацию о сопротивлении.
  /// </summary>
  public interface IHasResistance
  {
    /// <summary>
    /// Исходное представление сопротивления
    /// (например, "100 МОм").
    /// </summary>
    string? ResistanceSource { get; set; }
  }
}
