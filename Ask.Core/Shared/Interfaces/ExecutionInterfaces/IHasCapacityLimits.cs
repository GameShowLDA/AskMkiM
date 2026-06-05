namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  public interface IHasCapacityLimits
  {
    /// <summary>
    /// Единицы измерения электрической емкости.
    /// </summary>
    string? CapacityUnit { get; set; }

    /// <summary>
    /// Исходное представление нижней границы электрической емкости.
    /// </summary>
    string? LowerLimitCapacitySource { get; set; }

    /// <summary>
    /// Нижняя граница значения электрической емкости.
    /// </summary>
    double? LowerLimitCapacity { get; set; }

    /// <summary>
    /// Исходное представление верхней границы электрической емкости.
    /// </summary>
    string? HigherLimitCapacitySource { get; set; }

    /// <summary>
    /// Верхняя граница значения электрической емкости.
    /// </summary>
    double? HigherLimitCapacity { get; set; }
  }
}
