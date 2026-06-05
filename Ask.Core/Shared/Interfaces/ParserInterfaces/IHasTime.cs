namespace Ask.Core.Shared.Interfaces.ParserInterfaces
{
  /// <summary>
  /// Содержит информацию о времени.
  /// </summary>
  public interface IHasTime
  {
    double? Time { get; set; }

    /// <summary>
    /// Исходное представление времени
    /// (например, "100 мс", "5 с").
    /// </summary>
    string? TimeSource { get; set; }
  }

}
