using Ask.DataBase.Provider.Initialization;

namespace Ask.DataBase.Engine.Initialization;

/// <summary>
/// Точка входа слоя Engine для инициализации базы данных.
/// Нужна для того, чтобы внешний код работал только через Engine,
/// не обращаясь напрямую к Provider.
/// </summary>
public static class DatabaseEngineInitializer
{
  /// <summary>
  /// Запускает инициализацию базы данных через слой Provider.
  /// </summary>
  /// <param name="log">Необязательный callback для вывода промежуточных сообщений.</param>
  /// <param name="cancellationToken">Токен отмены.</param>
  /// <returns>Отчёт об инициализации базы данных.</returns>
  public static Task<DatabaseInitializationReport> InitializeAsync(
    Action<string>? log = null,
    CancellationToken cancellationToken = default)
  {
    return DatabaseInitializationService.InitializeAsync(log, cancellationToken);
  }
}
