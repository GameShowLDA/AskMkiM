namespace Ask.Core.Shared.Metadata.Atributes
{
  /// <summary>
  /// Атрибут, описывающий метрологическую информацию для команды —
  /// включая процентную и числовую погрешности и (при необходимости) диапазон измерений.
  /// </summary>
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
  public class CommandMeasurementAttribute : Attribute
  {
    /// <summary>
    /// Относительная (в процентах) погрешность по умолчанию.
    /// </summary>
    public double DefaultPercentage { get; }

    /// <summary>
    /// Абсолютная (числовая) погрешность по умолчанию.
    /// </summary>
    public double DefaultNumeric { get; }

    /// <summary>
    /// Нижняя граница диапазона, для которого применяются указанные погрешности.
    /// </summary>
    public double? DefaultMinRange { get; }

    /// <summary>
    /// Верхняя граница диапазона, для которого применяются указанные погрешности.
    /// Если <c>null</c> — диапазон распространяется до бесконечности.
    /// </summary>
    public double? DefaultMaxRange { get; }

    /// <summary>
    /// Создаёт новый экземпляр атрибута с диапазоном и погрешностями.
    /// </summary>
    /// <param name="percentage">Процентная погрешность по умолчанию.</param>
    /// <param name="numeric">Числовая погрешность по умолчанию.</param>
    /// <param name="minRange">Минимальное значение диапазона (опционально).</param>
    /// <param name="maxRange">Максимальное значение диапазона (опционально).</param>
    public CommandMeasurementAttribute(double percentage, double numeric, double? minRange = null, double? maxRange = null)
    {
      DefaultPercentage = percentage;
      DefaultNumeric = numeric;
      DefaultMinRange = minRange;
      DefaultMaxRange = maxRange;
    }
  }
}
