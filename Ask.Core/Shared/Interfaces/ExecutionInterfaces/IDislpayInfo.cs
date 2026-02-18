using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;

namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  /// <summary>
  /// Определяет функциональность по формированию читаемой информации для отображения данных в интерфейсе.
  /// </summary>
  public interface IDislpayInfo
  {
    /// <summary>
    /// Формирует строковое представление ошибочной цепочки точек
    /// на основании переданной модели цепочки.
    /// </summary>
    /// <param name="chain">Модель цепочки точек, содержащая данные об ошибке.</param>
    /// <returns>Строка, представляющая цепочку ошибок для отображения.</returns>
    string BuildErrorChainStringAsync(ChainModel chain);
  }
}
