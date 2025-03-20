namespace NewCore.Base.Function.ModuleVoltageCurrentSource
{
  /// <inheritdoc />
  public interface ICurrentManager
  {
    /// <summary>
    /// Устанавливает дискретное значение тока на МИНТ.
    /// </summary>
    /// <param name="integerPart">Целая часть значения тока.</param>
    /// <param name="decimalPart">Дробная часть значения тока.</param>
    /// <returns>Асинхронная задача.</returns>
    Task SetCurrentLevelAsync(int integerPart, int decimalPart);

    /// <summary>
    /// Устанавливает ограничение выдаваемого тока для модуля источника напряжения и тока (МИНТ).
    /// </summary>
    /// <param name="current">Ограничение тока в мА.</param>
    /// <returns>Булево значение, указывающее успешность операции.</returns>
    Task<bool> LimitationOfTheOutputCurrent(int current);
  }
}
