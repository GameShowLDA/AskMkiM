namespace NewCore.Base.Function.ManagerChassis
{
  /// <summary>
  /// Интерфейс для управления питанием шасси.
  /// </summary>
  public interface IPowerManagerChassis
  {
    /// <summary>
    /// Отключает питание шасси.
    /// </summary>
    /// <returns>Асинхронная задача.</returns>
    Task StopPowerAsync();

    /// <summary>
    /// Включает питание шасси.
    /// </summary>
    /// <returns>Асинхронная задача.</returns>
    Task StartPowerAsync();
  }
}
