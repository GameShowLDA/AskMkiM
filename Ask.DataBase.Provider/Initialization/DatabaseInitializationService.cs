using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Dictonary;
using Ask.DataBase.Provider.Configuration;
using Ask.DataBase.Provider.Context;
using Ask.DataBase.Provider.Services.Devices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
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
    }
    catch (SqliteException ex) when (IsDatabaseCorruptionException(ex))
    {
      LogException(ex, "[DB] Проверка SQLite не пройдена. База будет пересоздана");

      await RecreateDatabaseFileAsync(databasePath, report, progress);
    }

    await ApplySchemaAsync(databasePath, report, progress, cancellationToken);
    await EnsureLegacyCompatibilityModeColumnAsync(databasePath, report, progress, cancellationToken);
    await EnsureSettingsProtocolPrintColumnsAsync(databasePath, report, progress, cancellationToken);
    await EnsureFastMeterPpuDividerCoefficientColumnAsync(databasePath, report, progress, cancellationToken);
    await EnsureBreakdownTesterVoltageColumnsAsync(databasePath, report, progress, cancellationToken);
    await EnsureLegacyMkiHardwareProfilesStorageAsync(databasePath, report, progress, cancellationToken);
    await EnsureDefaultDataAsync(databasePath, report, progress, cancellationToken);

    TraceInfo(
      report,
      progress,
      $"[DB] Инициализация завершена. Применено миграций: {report.AppliedMigrations}, использован EnsureCreated: {report.UsedEnsureCreated}, добавлено горячих клавиш: {report.SeededHotkeys}, создано строк настроек: {report.CreatedDefaultSettingsRows}");
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
      .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
      .UseSqlite($"Data Source={databasePath}")
      .Options;

    await using var context = new AppDbContext(options);

    var migrations = context.Database.GetMigrations().ToList();

    if (migrations.Count > 0)
    {
      var appliedMigrations = await GetAppliedMigrationIdsSafeAsync(context.Database.GetDbConnection(), cancellationToken);

      if (await TryAdoptExistingSchemaAsync(context, migrations, report, progress, cancellationToken))
      {
        appliedMigrations = new HashSet<string>(migrations, StringComparer.OrdinalIgnoreCase);
        TraceWarning(
          report,
          progress,
          "[DB] Обнаружена существующая схема без истории миграций. Текущие миграции помечены как уже применённые.");
      }

      var pendingMigrations = migrations
        .Where(migration => !appliedMigrations.Contains(migration))
        .ToList();

      if (pendingMigrations.Count == 0)
      {
        TraceInfo(report, progress, "[DB] Ожидающих миграций нет.");
        return;
      }

      try
      {
        try
        {
          await context.Database.MigrateAsync(cancellationToken);
          report.AppliedMigrations = pendingMigrations.Count;
          TraceInfo(report, progress, $"[DB] Применены миграции: {string.Join(", ", pendingMigrations)}");
        }
        catch (SqliteException ex) when (IsExistingTablesException(ex))
        {
          if (await TryAdoptExistingSchemaAsync(context, migrations, report, progress, cancellationToken))
          {
            TraceWarning(
              report,
              progress,
              "[DB] Таблицы уже существуют без истории миграций. Миграции помечены как уже применённые.");
            return;
          }

          throw;
        }
      }
      catch (Exception ex) when (IsPendingModelChangesException(ex))
      {
        TraceWarning(
          report,
          progress,
          "[DB] Обнаружены незавершённые изменения модели EF. Миграции не применены, используется текущая схема базы.");
      }

      return;
    }

    await EnsureCreatedFallbackAsync(context, report, progress, cancellationToken);
  }

  private static async Task RecreateDatabaseFileAsync(
    string databasePath,
    DatabaseInitializationReport report,
    Action<string>? progress)
  {
    var damagedPath = $"{databasePath}.damaged_{DateTime.Now:yyyyMMdd_HHmmss}";
    if (File.Exists(databasePath))
    {
      await MoveFileWithRetryAsync(databasePath, damagedPath);
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
      .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
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
    var updatedHotkeys = 0;
    foreach (var pair in UiDictonary.DefaultsHotKeys)
    {
      if (existingHotkeys.TryGetValue(pair.Key, out var existingHotkey))
      {
        if (string.Equals(pair.Key, "CompareFile", StringComparison.OrdinalIgnoreCase)
          && string.Equals(existingHotkey.KeyCombination, "Ctrl+K", StringComparison.OrdinalIgnoreCase)
          && !string.Equals(existingHotkey.KeyCombination, pair.Value, StringComparison.OrdinalIgnoreCase))
        {
          existingHotkey.KeyCombination = pair.Value;
          updatedHotkeys++;
          TraceInfo(report, progress, $"[DB] Обновлена горячая клавиша по умолчанию: {pair.Key} -> {pair.Value}");
        }

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

    if (addedHotkeys > 0 || updatedHotkeys > 0)
    {
      await context.SaveChangesAsync(cancellationToken);
    }

    report.SeededHotkeys += addedHotkeys;
  }

  private static async Task EnsureLegacyCompatibilityModeColumnAsync(
    string databasePath,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync(cancellationToken);

    if (!await TableExistsAsync(connection, "Execution", cancellationToken)
      || await ColumnExistsAsync(connection, "Execution", "LegacyCompatibilityMode", cancellationToken))
    {
      return;
    }

    await using var command = connection.CreateCommand();
    command.CommandText =
      """
      ALTER TABLE "Execution"
      ADD COLUMN "LegacyCompatibilityMode" INTEGER NOT NULL DEFAULT 0;
      """;

    await command.ExecuteNonQueryAsync(cancellationToken);
    TraceWarning(
      report,
      progress,
      "[DB] Р’ СЃС‚Р°СЂРѕР№ С…РµРјРµ Execution РґРѕР±Р°РІР»РµРЅР° РєРѕР»РѕРЅРєР° LegacyCompatibilityMode.");
  }

  private static async Task EnsureSettingsProtocolPrintColumnsAsync(
    string databasePath,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync(cancellationToken);

    if (!await TableExistsAsync(connection, "SettingsProtocol", cancellationToken))
    {
      return;
    }

    await EnsureColumnAsync(connection, "SettingsProtocol", "PrintFontFamily", "TEXT NOT NULL DEFAULT 'Consolas'", report, progress, cancellationToken);
    await EnsureColumnAsync(connection, "SettingsProtocol", "PrintFontSize", "REAL NOT NULL DEFAULT 10.0", report, progress, cancellationToken);
  }

  private static async Task EnsureFastMeterPpuDividerCoefficientColumnAsync(
    string databasePath,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync(cancellationToken);

    if (!await TableExistsAsync(connection, "FastMeters", cancellationToken))
    {
      return;
    }

    await EnsureFastMeterPpuDividerCoefficientColumnAsync(
      connection,
      "AcwPpuDividerCoefficientPercent",
      report,
      progress,
      cancellationToken);

    await EnsureFastMeterPpuDividerCoefficientColumnAsync(
      connection,
      "DcwPpuDividerCoefficientPercent",
      report,
      progress,
      cancellationToken);
  }

  private static async Task EnsureFastMeterPpuDividerCoefficientColumnAsync(
    System.Data.Common.DbConnection connection,
    string columnName,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    if (await ColumnExistsAsync(connection, "FastMeters", columnName, cancellationToken))
    {
      return;
    }

    await using var command = connection.CreateCommand();
    command.CommandText =
      $"""
      ALTER TABLE "FastMeters"
      ADD COLUMN "{columnName}" REAL NOT NULL DEFAULT 100.0;
      """;

    await command.ExecuteNonQueryAsync(cancellationToken);
    TraceWarning(
      report,
      progress,
      $"[DB] Added FastMeters.{columnName} column for existing schema.");
  }

  /// <summary>
  /// Проверяет наличие колонок напряжений пробойной установки в старой схеме БД.
  /// </summary>
  private static async Task EnsureBreakdownTesterVoltageColumnsAsync(
    string databasePath,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync(cancellationToken);

    if (!await TableExistsAsync(connection, "BreakdownTesters", cancellationToken))
    {
      return;
    }

    await EnsureColumnAsync(connection, "BreakdownTesters", "PiMaxVoltage", "INTEGER NOT NULL DEFAULT 0", report, progress, cancellationToken);
    await EnsureColumnAsync(connection, "BreakdownTesters", "SiMaxVoltage", "INTEGER NOT NULL DEFAULT 0", report, progress, cancellationToken);
    await EnsureColumnAsync(connection, "BreakdownTesters", "IRMinVoltage", "INTEGER NOT NULL DEFAULT 0", report, progress, cancellationToken);
  }

  /// <summary>
  /// Добавляет колонку в таблицу, если она отсутствует.
  /// </summary>
  private static async Task EnsureColumnAsync(
    System.Data.Common.DbConnection connection,
    string tableName,
    string columnName,
    string columnDefinition,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    if (await ColumnExistsAsync(connection, tableName, columnName, cancellationToken))
    {
      return;
    }

    await using var command = connection.CreateCommand();
    command.CommandText =
      $"""
      ALTER TABLE "{tableName}"
      ADD COLUMN "{columnName}" {columnDefinition};
      """;

    await command.ExecuteNonQueryAsync(cancellationToken);
    TraceWarning(
      report,
      progress,
      $"[DB] В таблицу {tableName} добавлена колонка {columnName} для совместимости со старой схемой.");
  }

  /// <summary>
  /// Проверяет и создает таблицу legacy-профилей аппаратуры АСК-МКИ в текущей базе данных.
  /// </summary>
  public static async Task EnsureLegacyMkiHardwareProfilesStorageAsync(CancellationToken cancellationToken = default)
  {
    var databasePath = DbPathResolver.Resolve();
    EnsureDirectoryExists(databasePath);
    await EnsureLegacyMkiHardwareProfilesStorageAsync(databasePath, null, null, cancellationToken);
  }

  /// <summary>
  /// Проверяет таблицу legacy-профилей аппаратуры АСК-МКИ и добавляет недостающие элементы схемы.
  /// </summary>
  private static async Task EnsureLegacyMkiHardwareProfilesStorageAsync(
    string databasePath,
    DatabaseInitializationReport? report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync(cancellationToken);

    await using (var createCommand = connection.CreateCommand())
    {
      createCommand.CommandText = LegacyMkiHardwareProfileStorageSql.CreateTableSql;
      await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    await EnsureLegacyMkiHardwareProfileColumnsAsync(connection, report, progress, cancellationToken);

    await using (var indexCommand = connection.CreateCommand())
    {
      indexCommand.CommandText = LegacyMkiHardwareProfileStorageSql.CreateIndexSql;
      await indexCommand.ExecuteNonQueryAsync(cancellationToken);
    }
  }

  /// <summary>
  /// Добавляет недостающие колонки в таблицу legacy-профилей аппаратуры АСК-МКИ.
  /// </summary>
  private static async Task EnsureLegacyMkiHardwareProfileColumnsAsync(
    System.Data.Common.DbConnection connection,
    DatabaseInitializationReport? report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    foreach (var column in LegacyMkiHardwareProfileStorageSql.Columns)
    {
      if (await ColumnExistsAsync(connection, LegacyMkiHardwareProfileStorageSql.TableName, column.Key, cancellationToken))
      {
        continue;
      }

      await using var command = connection.CreateCommand();
      command.CommandText =
        $"""
        ALTER TABLE "{LegacyMkiHardwareProfileStorageSql.TableName}"
        ADD COLUMN "{column.Key}" {column.Value};
        """;

      await command.ExecuteNonQueryAsync(cancellationToken);
      if (report != null)
      {
        TraceWarning(
          report,
          progress,
          $"[DB] В таблицу {LegacyMkiHardwareProfileStorageSql.TableName} добавлена колонка {column.Key}.");
      }
    }
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

  private static async Task EnsureCreatedFallbackAsync(
    AppDbContext context,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
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

  private static bool IsPendingModelChangesException(Exception ex)
  {
    var message = ex.Message ?? string.Empty;

    return message.Contains(nameof(RelationalEventId.PendingModelChangesWarning), StringComparison.Ordinal)
      || message.Contains("pending changes", StringComparison.OrdinalIgnoreCase)
      || message.Contains("незаверш", StringComparison.OrdinalIgnoreCase);
  }

  private static bool IsExistingTablesException(SqliteException ex)
  {
    return (ex.Message ?? string.Empty).Contains("already exists", StringComparison.OrdinalIgnoreCase);
  }

  private static bool IsDatabaseCorruptionException(SqliteException ex)
  {
    var message = ex.Message ?? string.Empty;

    return ex.SqliteErrorCode == 11
      || message.Contains("file is not a database", StringComparison.OrdinalIgnoreCase)
      || message.Contains("database disk image is malformed", StringComparison.OrdinalIgnoreCase)
      || message.Contains("disk i/o error", StringComparison.OrdinalIgnoreCase);
  }

  private static async Task<bool> TryAdoptExistingSchemaAsync(
    AppDbContext context,
    IReadOnlyCollection<string> migrations,
    DatabaseInitializationReport report,
    Action<string>? progress,
    CancellationToken cancellationToken)
  {
    if (migrations.Count == 0)
    {
      return false;
    }

    var connection = context.Database.GetDbConnection();
    var closeConnection = false;

    if (connection.State != System.Data.ConnectionState.Open)
    {
      await connection.OpenAsync(cancellationToken);
      closeConnection = true;
    }

    try
    {
      var hasApplicationTables = false;
      foreach (var tableName in GetApplicationTableNames(context))
      {
        if (await TableExistsAsync(connection, tableName, cancellationToken))
        {
          hasApplicationTables = true;
          break;
        }
      }

      if (!hasApplicationTables)
      {
        return false;
      }

      var historyTableExists = await TableExistsAsync(connection, "__EFMigrationsHistory", cancellationToken);
      var appliedMigrations = historyTableExists
        ? await GetAppliedMigrationIdsAsync(connection, cancellationToken)
        : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      if (historyTableExists && appliedMigrations.Count >= migrations.Count)
      {
        return false;
      }

      await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

      await using (var createCommand = connection.CreateCommand())
      {
        createCommand.Transaction = transaction;
        createCommand.CommandText =
          """
          CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
              "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
              "ProductVersion" TEXT NOT NULL
          );
          """;

        await createCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      foreach (var migration in migrations)
      {
        await using var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;
        insertCommand.CommandText =
          """
          INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
          VALUES ($migrationId, $productVersion);
          """;
        var migrationIdParameter = insertCommand.CreateParameter();
        migrationIdParameter.ParameterName = "$migrationId";
        migrationIdParameter.Value = migration;
        insertCommand.Parameters.Add(migrationIdParameter);

        var productVersionParameter = insertCommand.CreateParameter();
        productVersionParameter.ParameterName = "$productVersion";
        productVersionParameter.Value = "9.0.4";
        insertCommand.Parameters.Add(productVersionParameter);

        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      await transaction.CommitAsync(cancellationToken);
      TraceWarning(report, progress, "[DB] История миграций создана для уже существующей схемы.");
      return true;
    }
    finally
    {
      if (closeConnection)
      {
        await connection.CloseAsync();
      }
    }
  }

  private static IEnumerable<string> GetApplicationTableNames(AppDbContext context)
  {
    return context.Model
      .GetEntityTypes()
      .Select(entityType => entityType.GetTableName())
      .Where(tableName => !string.IsNullOrWhiteSpace(tableName))
      .Distinct(StringComparer.OrdinalIgnoreCase)!
      .Cast<string>();
  }

  private static async Task<bool> TableExistsAsync(
    System.Data.Common.DbConnection connection,
    string tableName,
    CancellationToken cancellationToken)
  {
    await using var command = connection.CreateCommand();
    command.CommandText =
      """
      SELECT 1
      FROM sqlite_master
      WHERE type = 'table' AND name = $tableName
      LIMIT 1;
      """;
    var parameter = command.CreateParameter();
    parameter.ParameterName = "$tableName";
    parameter.Value = tableName;
    command.Parameters.Add(parameter);

    var result = await command.ExecuteScalarAsync(cancellationToken);
    return result is not null;
  }

  private static async Task<bool> ColumnExistsAsync(
    System.Data.Common.DbConnection connection,
    string tableName,
    string columnName,
    CancellationToken cancellationToken)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = $"PRAGMA table_info(\"{tableName.Replace("\"", "\"\"")}\");";

    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken))
    {
      if (!reader.IsDBNull(1)
        && string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

  private static async Task<HashSet<string>> GetAppliedMigrationIdsSafeAsync(
    System.Data.Common.DbConnection connection,
    CancellationToken cancellationToken)
  {
    var closeConnection = false;

    if (connection.State != System.Data.ConnectionState.Open)
    {
      await connection.OpenAsync(cancellationToken);
      closeConnection = true;
    }

    try
    {
      if (!await TableExistsAsync(connection, "__EFMigrationsHistory", cancellationToken))
      {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      }

      return await GetAppliedMigrationIdsAsync(connection, cancellationToken);
    }
    finally
    {
      if (closeConnection)
      {
        await connection.CloseAsync();
      }
    }
  }

  private static async Task<HashSet<string>> GetAppliedMigrationIdsAsync(
    System.Data.Common.DbConnection connection,
    CancellationToken cancellationToken)
  {
    var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    await using var command = connection.CreateCommand();
    command.CommandText =
      """
      SELECT "MigrationId"
      FROM "__EFMigrationsHistory";
      """;

    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken))
    {
      if (!reader.IsDBNull(0))
      {
        result.Add(reader.GetString(0));
      }
    }

    return result;
  }

  private static async Task MoveFileWithRetryAsync(string sourcePath, string destinationPath)
  {
    const int retryCount = 5;

    for (var attempt = 1; attempt <= retryCount; attempt++)
    {
      try
      {
        File.Move(sourcePath, destinationPath, overwrite: true);
        return;
      }
      catch (IOException) when (attempt < retryCount)
      {
        await Task.Delay(150 * attempt);
      }
    }

    File.Move(sourcePath, destinationPath, overwrite: true);
  }
}
