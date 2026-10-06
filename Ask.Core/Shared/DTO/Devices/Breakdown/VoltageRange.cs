namespace Ask.Core.Shared.DTO.Devices.Breakdown
{
  /// <summary>
  /// Содержит границы и шаг установки напряжения пробойной установки, а также допустимые значения вне сетки шага.
  /// </summary>
  public class VoltageRange
  {
    /// <summary>
    /// Минимальное допустимое напряжение (В).
    /// </summary>
    public double MinVoltage { get; set; }

    /// <summary>
    /// Максимальное допустимое напряжение системы (В).
    /// </summary>
    public double MaxVoltage { get; set; }

    /// <summary>
    /// Шаг установки напряжения от минимального значения (В).
    /// </summary>
    public double Step { get; set; }

    /// <summary>
    /// Допустимые напряжения внутри диапазона, не соответствующие сетке шага (В).
    /// </summary>
    public List<double> Exceptions { get; set; } = [];

    /// <summary>
    /// Создаёт независимую копию диапазона и списка исключений.
    /// </summary>
    /// <returns>Копия параметров диапазона напряжения.</returns>
    public VoltageRange Clone() => new()
    {
      MinVoltage = MinVoltage,
      MaxVoltage = MaxVoltage,
      Step = Step,
      Exceptions = [.. Exceptions],
    };

    /// <summary>
    /// Проверяет напряжение по границам диапазона, сетке шага и списку исключений.
    /// </summary>
    /// <param name="voltage">Устанавливаемое напряжение (В).</param>
    /// <returns><see langword="true"/>, если напряжение допустимо; иначе <see langword="false"/>.</returns>
    public bool IsAllowed(double voltage)
    {
      if (!double.IsFinite(voltage) || !double.IsFinite(MinVoltage) || !double.IsFinite(MaxVoltage)
          || !double.IsFinite(Step) || Step <= 0 || voltage < MinVoltage || voltage > MaxVoltage)
        return false;

      if (Exceptions?.Contains(voltage) == true)
        return true;

      double steps = (voltage - MinVoltage) / Step;
      return Math.Abs(steps - Math.Round(steps)) < 1e-9;
    }
  }
}
