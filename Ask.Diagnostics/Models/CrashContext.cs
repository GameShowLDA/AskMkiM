using System.Collections.Concurrent;

namespace Ask.Diagnostics.Models
{
  /// <summary>
  /// Содержит контекст возникновения аварийной ситуации,
  /// используемый при формировании диагностического пакета.
  /// </summary>
  public sealed class CrashContext
  {
    /// <summary>
    /// Коллекция ошибок, возникших при сборе диагностических данных.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _collectorFailures = new();

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="CrashContext"/>.
    /// </summary>
    /// <param name="exception">Исключение, вызвавшее аварийное завершение.</param>
    /// <param name="timestamp">Дата и время возникновения ошибки.</param>
    /// <param name="rootDirectory">Корневой каталог приложения.</param>
    /// <param name="packageDirectory">Каталог формирования диагностического пакета.</param>
    /// <param name="packageName">Имя диагностического пакета.</param>
    public CrashContext(
      Exception exception,
      DateTimeOffset timestamp,
      string rootDirectory,
      string packageDirectory,
      string packageName)
    {
      Exception = exception ?? throw new ArgumentNullException(nameof(exception));
      Timestamp = timestamp;
      RootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
      PackageDirectory = packageDirectory ?? throw new ArgumentNullException(nameof(packageDirectory));
      PackageName = packageName ?? throw new ArgumentNullException(nameof(packageName));
    }

    /// <summary>
    /// Исключение, вызвавшее аварийное завершение приложения.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Дата и время возникновения ошибки.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Корневой каталог приложения.
    /// </summary>
    public string RootDirectory { get; }

    /// <summary>
    /// Каталог, в котором формируется диагностический пакет.
    /// </summary>
    public string PackageDirectory { get; }

    /// <summary>
    /// Имя диагностического пакета.
    /// </summary>
    public string PackageName { get; }

    /// <summary>
    /// Сведения об ошибках, возникших при сборе диагностической информации.
    /// </summary>
    public IReadOnlyDictionary<string, string> CollectorFailures => _collectorFailures;

    /// <summary>
    /// Путь к сформированному ZIP-архиву диагностического пакета.
    /// </summary>
    public string? ZipPath { get; set; }

    /// <summary>
    /// Добавляет информацию об ошибке, возникшей во время работы сборщика данных.
    /// </summary>
    /// <param name="collectorName">Имя сборщика диагностических данных.</param>
    /// <param name="exception">Возникшее исключение.</param>
    public void AddCollectorFailure(string collectorName, Exception exception)
    {
      _collectorFailures[collectorName] = exception.ToString();
    }
  }
}
