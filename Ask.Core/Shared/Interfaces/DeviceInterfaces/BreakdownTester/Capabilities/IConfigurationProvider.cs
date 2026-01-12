namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities
{
  /// <summary>
  /// Универсальный интерфейс для режимов, поддерживающих работу с конфигурацией.
  /// </summary>
  public interface IConfigurationProvider<T>
  {
    /// <summary>
    /// Считывает текущую конфигурацию режима.
    /// </summary>
    /// <returns>
    /// Объект типа <typeparamref name="T"/>, содержащий актуальные параметры конфигурации.
    /// </returns>
    Task<T> ReadConfigurationAsync();

    /// <summary>
    /// Сбрасывает все параметры режима в значения по умолчанию.
    /// </summary>
    void ResetConfiguration();
  }
}
