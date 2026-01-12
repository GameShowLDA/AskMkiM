namespace Ask.Core.Shared.DTO.Executor.MeasurementError
{
  /// <summary>
  /// Диапазон измерений и соответствующие ему абсолютная и относительная погрешности.
  /// </summary>
  public class MeasurementErrorRangeEntity
  {
    /// <summary>Первичный ключ.</summary>
    public int Id { get; set; }

    /// <summary>Нижняя граница диапазона (включительно).</summary>
    public double MinValue { get; set; }

    /// <summary>
    /// Верхняя граница диапазона (исключительно). Если null — без верхней границы (до бесконечности).
    /// </summary>
    public double? MaxValue { get; set; }

    /// <summary>Абсолютная (числовая) погрешность.</summary>
    public double NumericError { get; set; }

    /// <summary>Относительная (в процентах) погрешность.</summary>
    public double PercentageError { get; set; }

    /// <summary>FK на MeasurementErrorEntity.</summary>
    public int MeasurementErrorEntityId { get; set; }

    /// <summary>Навигационное свойство на родителя.</summary>
    public MeasurementErrorEntity MeasurementErrorEntity { get; set; } = null!;
  }
}
