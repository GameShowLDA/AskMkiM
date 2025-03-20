namespace NewCore.Base.Function.DBC
{
  /// <summary>
  /// Интерфейс для управления состоянием устройства коммутации шин.
  /// </summary>
  public interface IStateDeviceBusCommutation
  {
    /// <summary>
    /// Выполняет сброс устройства коммутации шин к исходному состоянию.
    /// </summary>
    /// <returns>Возвращает <c>true</c>, если сброс успешно выполнен, иначе <c>false</c>.</returns>
    Task<bool> ResetAsync();

    /// <summary>
    /// Инициализирует устройство коммутации шин.
    /// </summary>
    /// <returns>
    /// Кортеж, содержащий флаг успешного подключения (<c>true</c> или <c>false</c>) 
    /// и строку с ответом устройства.
    /// </returns>
    Task<(bool Connect, string Answer)> Initialize();
  }
}
