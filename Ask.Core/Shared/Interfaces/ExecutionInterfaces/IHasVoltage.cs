namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  /// <summary>
  /// Содержит информацию о напряжении.
  /// </summary>
  public interface IHasVoltage
  {
    /// <summary>
    /// Исходное представление напряжения
    /// (например, "220 В").
    /// </summary>
    string? VoltageSource { get; set; }
  }
}
