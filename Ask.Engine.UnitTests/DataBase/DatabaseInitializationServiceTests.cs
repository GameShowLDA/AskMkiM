using Ask.DataBase.Provider.Context;
using Ask.DataBase.Provider.Initialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Ask.Engine.UnitTests.DataBase;

public sealed class DatabaseInitializationServiceTests
{
  [Fact]
  public async Task TryAdoptExistingSchemaAsync_DoesNotAdoptPendingMigrationsWhenHistoryExists()
  {
    string directoryPath = Path.Combine(
      Path.GetTempPath(),
      $"AskMkiM-database-initialization-{Guid.NewGuid():N}");
    string databasePath = Path.Combine(directoryPath, "app.db");
    Directory.CreateDirectory(directoryPath);

    try
    {
      await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
      {
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
          """
          CREATE TABLE PowerSupplies (Id INTEGER PRIMARY KEY);
          CREATE TABLE __EFMigrationsHistory (MigrationId TEXT PRIMARY KEY, ProductVersion TEXT NOT NULL);
          INSERT INTO __EFMigrationsHistory VALUES ('FirstMigration', '9.0.4');
          """;
        await command.ExecuteNonQueryAsync();
      }

      var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={databasePath}")
        .Options;
      await using var context = new AppDbContext(options);
      var report = new DatabaseInitializationReport();

      bool adopted = await DatabaseInitializationService.TryAdoptExistingSchemaAsync(
        context,
        ["FirstMigration", "PendingMigration"],
        report,
        progress: null,
        CancellationToken.None);

      Assert.False(adopted);
      await using var checkConnection = new SqliteConnection($"Data Source={databasePath}");
      await checkConnection.OpenAsync();
      await using var checkCommand = checkConnection.CreateCommand();
      checkCommand.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory;";
      Assert.Equal(1L, await checkCommand.ExecuteScalarAsync());
    }
    finally
    {
      SqliteConnection.ClearAllPools();
      Directory.Delete(directoryPath, recursive: true);
    }
  }

  [Fact]
  public async Task EnsureDeviceDisplayDelayMessagesColumnAsync_AddsMissingColumnWithoutChangingSettings()
  {
    string directoryPath = Path.Combine(
      Path.GetTempPath(),
      $"AskMkiM-database-initialization-{Guid.NewGuid():N}");
    string databasePath = Path.Combine(directoryPath, "app.db");
    Directory.CreateDirectory(directoryPath);

    try
    {
      await CreateLegacyDeviceDisplaySettingsAsync(databasePath);

      var report = new DatabaseInitializationReport();
      await DatabaseInitializationService.EnsureDeviceDisplayDelayMessagesColumnAsync(
        databasePath,
        report,
        progress: null,
        CancellationToken.None);

      await using var connection = new SqliteConnection($"Data Source={databasePath}");
      await connection.OpenAsync();

      await using var command = connection.CreateCommand();
      command.CommandText = """
        SELECT
          ShowMachineAddresses,
          ShowConnectionInfo,
          ShowDeviceExecutionParameters,
          ShowMeasurementResults,
          ShowIntermediateMeasurementResults,
          ShowDelayMessages
        FROM DeviceDisplaySettings
        WHERE Id = 1;
        """;

      await using var reader = await command.ExecuteReaderAsync();
      Assert.True(await reader.ReadAsync());
      Assert.False(reader.GetBoolean(0));
      Assert.True(reader.GetBoolean(1));
      Assert.False(reader.GetBoolean(2));
      Assert.True(reader.GetBoolean(3));
      Assert.False(reader.GetBoolean(4));
      Assert.True(reader.GetBoolean(5));
    }
    finally
    {
      SqliteConnection.ClearAllPools();
      Directory.Delete(directoryPath, recursive: true);
    }
  }

  [Fact]
  public async Task EnsureDeviceDisplayDelayMessagesColumnAsync_IsIdempotent()
  {
    string directoryPath = Path.Combine(
      Path.GetTempPath(),
      $"AskMkiM-database-initialization-{Guid.NewGuid():N}");
    string databasePath = Path.Combine(directoryPath, "app.db");
    Directory.CreateDirectory(directoryPath);

    try
    {
      await CreateLegacyDeviceDisplaySettingsAsync(databasePath);
      var report = new DatabaseInitializationReport();

      await DatabaseInitializationService.EnsureDeviceDisplayDelayMessagesColumnAsync(
        databasePath,
        report,
        progress: null,
        CancellationToken.None);
      await DatabaseInitializationService.EnsureDeviceDisplayDelayMessagesColumnAsync(
        databasePath,
        report,
        progress: null,
        CancellationToken.None);

      await using var connection = new SqliteConnection($"Data Source={databasePath}");
      await connection.OpenAsync();

      await using var command = connection.CreateCommand();
      command.CommandText = """
        SELECT COUNT(*)
        FROM pragma_table_info('DeviceDisplaySettings')
        WHERE name = 'ShowDelayMessages';
        """;

      Assert.Equal(1L, await command.ExecuteScalarAsync());
    }
    finally
    {
      SqliteConnection.ClearAllPools();
      Directory.Delete(directoryPath, recursive: true);
    }
  }

  private static async Task CreateLegacyDeviceDisplaySettingsAsync(string databasePath)
  {
    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = """
      CREATE TABLE DeviceDisplaySettings (
        Id INTEGER NOT NULL CONSTRAINT PK_DeviceDisplaySettings PRIMARY KEY AUTOINCREMENT,
        ShowMachineAddresses INTEGER NOT NULL,
        ShowConnectionInfo INTEGER NOT NULL,
        ShowDeviceExecutionParameters INTEGER NOT NULL,
        ShowMeasurementResults INTEGER NOT NULL,
        ShowIntermediateMeasurementResults INTEGER NOT NULL
      );

      INSERT INTO DeviceDisplaySettings (
        Id,
        ShowMachineAddresses,
        ShowConnectionInfo,
        ShowDeviceExecutionParameters,
        ShowMeasurementResults,
        ShowIntermediateMeasurementResults)
      VALUES (1, 0, 1, 0, 1, 0);
      """;
    await command.ExecuteNonQueryAsync();
  }
}
