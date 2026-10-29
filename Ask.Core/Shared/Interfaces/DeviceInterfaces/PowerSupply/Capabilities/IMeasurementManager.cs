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
    /// <returns>Измеренное напряжение в вольтах.</returns>
    Task<decimal> MeasureVoltageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Измеряет выходной ток.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Измеренный ток в амперах.</returns>
    Task<decimal> MeasureCurrentAsync(CancellationToken cancellationToken = default);
  }
}
