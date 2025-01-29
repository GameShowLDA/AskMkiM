namespace AppConfig.Config
{
  public static class LoopConfig
  {
    /// <summary>
    /// Флаг, указывающий, активен ли режим циклического измерения.
    /// </summary>
    static private bool IsLoopMeasurementActive { get; set; }

    /// <summary>
    /// Устанавливает режим циклического измерения.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static async Task SetLoopMeasurement(bool enable)
    {
      await Task.Run(() =>
      {
        IsLoopMeasurementActive = enable;
      });
    }

    /// <summary>
    /// Возвращает статус режима циклического измерения.
    /// </summary>
    /// <returns>true, если включен; false, если выключен.</returns>
    static public async Task<bool> GetIsLoopMeasurementEnabled() => await Task.Run(() => IsLoopMeasurementActive);
  }
}
