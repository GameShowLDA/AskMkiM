using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities
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
    Task SetCurrentLevelAsync(int integerPart, int decimalPart, IUserInteractionService? messageService = null);

    /// <summary>
    /// Устанавливает ограничение выдаваемого тока для модуля источника напряжения и тока (МИНТ).
    /// </summary>
    /// <param name="current">Ограничение тока в мА.</param>
    /// <returns>Булево значение, указывающее успешность операции.</returns>
    Task<bool> LimitationOfTheOutputCurrent(int current, IUserInteractionService? messageService = null);
  }
}
