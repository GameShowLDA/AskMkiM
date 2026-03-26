using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Dictonary;
using Ask.DataBase.Provider.Configuration;
using Ask.DataBase.Provider.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static Ask.LogLib.LoggerUtility;

namespace Ask.DataBase.Provider.Initialization;

/// <summary>
/// Сервис инициализации базы данных.
/// Проверяет целостность SQLite-файла, применяет миграции или создаёт схему,
/// а затем добавляет обязательные данные по умолчанию.
/// </summary>
public static class DatabaseInitializationService
{
  /// <summary>
  /// Инициализирует базу данных и возвращает подробный отчёт.
  /// </summary>
  /// <param name="progress">Необязательный callback для промежуточных сообщений.</param>
  /// <param name="cancellationToken">Токен отмены.</param>
  /// <returns>Отчёт об инициализации базы данных.</returns>
  public static async Task<DatabaseInitializationReport> InitializeAsync(
    Action<string>? progress = null,
    CancellationToken cancellationToken = default)
  {
    var databasePath = DbPathResolver.Resolve();
    var report = new DatabaseInitializationReport
    {
      DatabasePath = databasePath,
      DatabaseAlreadyExisted = File.Exists(databasePath),
    };

    TraceInfo(report, progress, $"[DB] Путь к базе данных: {databasePath}");

    try
    {
      EnsureDirectoryExists(databasePath);
      await InitializeInternalAsync(databasePath, report, progress, cancellationToken);
      return report;
    }
    catch (Exception ex)
    {
      LogException(ex, "[DB] Ошибка инициализации базы данных");
      throw;
    }
  }

  private static async Task InitializeInternalAsync(
    string databasePath,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    try
    {
      await ValidateSqliteFileAsync(databasePath, report, progress, cancellationToken);
      await ApplySchemaAsync(databasePath, report, progress, cancellationToken);
      await EnsureDefaultDataAsync(databasePath, report, progress, cancellationToken);

      TraceInfo(
        report,
        progress,
        $"[DB] Инициализация завершена. Применено миграций: {report.AppliedMigrations}, использован EnsureCreated: {report.UsedEnsureCreated}, добавлено горячих клавиш: {report.SeededHotkeys}, создано строк настроек: {report.CreatedDefaultSettingsRows}");
    }
    catch (SqliteException ex)
    {
      LogException(ex, "[DB] Проверка SQLite не пройдена. База будет пересоздана");

      await RecreateDatabaseFileAsync(databasePath, report, progress);
      await ApplySchemaAsync(databasePath, report, progress, cancellationToken);
      await EnsureDefaultDataAsync(databasePath, report, progress, cancellationToken);

      TraceWarning(report, progress, "[DB] База данных пересоздана после ошибки SQLite.");
    }
  }

  private static void EnsureDirectoryExists(string databasePath)
  {
    var directory = Path.GetDirectoryName(databasePath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
      Directory.CreateDirectory(directory);
    }
  }

  private static async Task ValidateSqliteFileAsync(
    string databasePath,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    if (!File.Exists(databasePath))
    {
      report.DatabaseCreated = true;
      TraceInfo(report, progress, "[DB] Файл базы данных отсутствует. Будет создана новая база.");
      return;
    }

    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync(cancellationToken);

    await using var command = connection.CreateCommand();
    command.CommandText = "PRAGMA integrity_check;";

    var result = (await command.ExecuteScalarAsync(cancellationToken))?.ToString();
    if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
    {
      throw new SqliteException($"Проверка целостности не пройдена: {result}", 11);
    }

    TraceInfo(report, progress, "[DB] Проверка целостности SQLite пройдена.");
  }

  private static async Task ApplySchemaAsync(
    string databasePath,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseSqlite($"Data Source={databasePath}")
      .Options;

    await using var context = new AppDbContext(options);

    var migrations = context.Database.GetMigrations().ToList();
    var pendingMigrations = context.Database.GetPendingMigrations().ToList();

    if (migrations.Count > 0)
    {
      if (pendingMigrations.Count == 0)
      {
        TraceInfo(report, progress, "[DB] Ожидающих миграций нет.");
      }
      else
      {
        await context.Database.MigrateAsync(cancellationToken);
        report.AppliedMigrations = pendingMigrations.Count;
        TraceInfo(report, progress, $"[DB] Применены миграции: {string.Join(", ", pendingMigrations)}");
      }

      return;
    }

    var created = await context.Database.EnsureCreatedAsync(cancellationToken);
    report.UsedEnsureCreated = true;

    if (created)
    {
      TraceInfo(report, progress, "[DB] Схема базы создана через EnsureCreated.");
    }
    else
    {
      TraceInfo(report, progress, "[DB] Схема уже существует. EnsureCreated не внёс изменений.");
    }
  }

  private static async Task RecreateDatabaseFileAsync(
    string databasePath,
    DatabaseInitializationReport report,
    Action<string>? progress)
  {
    await Task.CompletedTask;

    var damagedPath = $"{databasePath}.damaged_{DateTime.Now:yyyyMMdd_HHmmss}";
    if (File.Exists(databasePath))
    {
      File.Move(databasePath, damagedPath, overwrite: true);
      TraceWarning(report, progress, $"[DB] Повреждённая база перемещена в: {damagedPath}");
    }

    report.DatabaseRecreated = true;
    report.DatabaseCreated = true;
  }

  private static async Task EnsureDefaultDataAsync(
    string databasePath,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseSqlite($"Data Source={databasePath}")
      .Options;

    await using var context = new AppDbContext(options);

    report.CreatedDefaultSettingsRows += await EnsureSingleSettingsRowAsync(
      context,
      context.SettingsProtocol,
      "SettingsProtocol",
      static () => new SettingsProtocolDto(),
      report,
      progress,
      cancellationToken);

    report.CreatedDefaultSettingsRows += await EnsureSingleSettingsRowAsync(
      context,
      context.Execution,
      "Execution",
      static () => new SettingsExecutionDto(),
      report,
      progress,
      cancellationToken);

    report.CreatedDefaultSettingsRows += await EnsureSingleSettingsRowAsync(
      context,
      context.UserInterface,
      "UserInterface",
      static () => new UserInterfaceDto(),
      report,
      progress,
      cancellationToken);

    report.CreatedDefaultSettingsRows += await EnsureSingleSettingsRowAsync(
      context,
      context.DeviceDisplaySettings,
      "DeviceDisplaySettings",
      static () => new DeviceDisplaySettingsDto(),
      report,
      progress,
      cancellationToken);

    var existingHotkeys = (await context.FileHotKeys.ToListAsync(cancellationToken))
      .ToDictionary(x => x.ActionName, StringComparer.OrdinalIgnoreCase);

    var addedHotkeys = 0;
    foreach (var pair in UiDictonary.DefaultsHotKeys)
    {
      if (existingHotkeys.ContainsKey(pair.Key))
      {
        continue;
      }

      context.FileHotKeys.Add(new FileHotkeyDto
      {
        ActionName = pair.Key,
        KeyCombination = pair.Value,
        IsEnabled = true,
        Description = pair.Key,
      });

      addedHotkeys++;
      TraceInfo(report, progress, $"[DB] Добавлена горячая клавиша по умолчанию: {pair.Key} -> {pair.Value}");
    }

    if (addedHotkeys > 0)
    {
      await context.SaveChangesAsync(cancellationToken);
    }

    report.SeededHotkeys += addedHotkeys;
  }

  private static async Task<int> EnsureSingleSettingsRowAsync<T>(
    AppDbContext context,
    DbSet<T> dbSet,
    string tableName,
    Func<T> factory,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
    where T : class
  {
    var items = await dbSet.ToListAsync(cancellationToken);

    if (items.Count == 0)
    {
      dbSet.Add(factory());
      await context.SaveChangesAsync(cancellationToken);
      TraceInfo(report, progress, $"[DB] Таблица {tableName} была пустой. Добавлена строка по умолчанию.");
      return 1;
    }

    if (items.Count > 1)
    {
      var extraItems = items.Skip(1).ToList();
      dbSet.RemoveRange(extraItems);
      await context.SaveChangesAsync(cancellationToken);
      TraceWarning(report, progress, $"[DB] Таблица {tableName} содержала лишние строки. Оставлена одна запись.");
    }

    return 0;
  }

  private static void TraceInfo(DatabaseInitializationReport report, Action<string>? progress, string message)
  {
    report.Messages.Add(message);
    LogInformation(message);
    progress?.Invoke(message);
  }

  private static void TraceWarning(DatabaseInitializationReport report, Action<string>? progress, string message)
  {
    report.Messages.Add(message);
    LogWarning(message);
    progress?.Invoke(message);
  }
}
