namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities
{
  /// <summary>
  /// Предоставляет управление уставкой ограничения тока источника питания.
  /// </summary>
  public interface ICurrentManager
  {
    /// <summary>
    /// Минимальная допустимая уставка ограничения тока в амперах.
    /// </summary>
    decimal MinCurrent { get; }

    /// <summary>
    /// Максимальная допустимая уставка ограничения тока в амперах.
    /// </summary>
    decimal MaxCurrent { get; }

    /// <summary>
    /// Программный шаг уставки тока в амперах, отсчитываемый от <see cref="MinCurrent"/>.
    /// </summary>
    decimal CurrentStep { get; }

    /// <summary>
    /// Устанавливает ограничение тока в допустимом диапазоне с программным шагом.
    /// </summary>
    /// <param name="current">Уставка ограничения тока в амперах, кратная шагу относительно минимальной уставки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача установки ограничения тока.</returns>
    Task SetCurrentAsync(decimal current, CancellationToken cancellationToken = default);

    /// <summary>
    /// Читает уставку ограничения тока источника питания.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Уставка ограничения тока в амперах.</returns>
    Task<decimal> GetCurrentAsync(CancellationToken cancellationToken = default);
  }
}
