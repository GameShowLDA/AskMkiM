using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities
{
  /// <summary>
  /// Предоставляет измерения напряжения и тока на выходе источника питания.
  /// </summary>
  public interface IMeasurementManager
  {
    /// <summary>
    /// Измеряет выходное напряжение.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Измеренное напряжение в вольтах.</returns>
    Task<decimal> MeasureVoltageAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Измеряет выходной ток.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Измеренный ток в амперах.</returns>
    Task<decimal> MeasureCurrentAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null);
  }
}
