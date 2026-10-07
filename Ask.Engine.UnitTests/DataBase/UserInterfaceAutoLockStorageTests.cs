using Ask.Core.Shared.DTO.Settings;
using Ask.DataBase.Provider.Context;
using Ask.DataBase.Provider.Initialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ask.Engine.UnitTests.DataBase;

public sealed class UserInterfaceAutoLockStorageTests
{
  [Fact]
  public async Task MigrationKeepsExistingSettingsAndNewDefaultIsDisabled()
  {
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
    await context.GetService<IMigrator>().MigrateAsync("20261006045223_AddBreakdownVoltageRanges");
    await context.Database.ExecuteSqlRawAsync("""
      INSERT INTO UserInterface (Language, Theme, UseChainPointBodyBackgroundHighlighting,
        UseCommandAutoCollapse, UseCommandBodyBackgroundHighlighting, UseStyleErrorUnderlining,
        UseSyntaxErrorUnderlining, UseSyntaxHighlighting, UseTopMenuIcons)
      VALUES ('en', 1, 0, 0, 0, 1, 1, 0, 1);
      """);
    await context.Database.MigrateAsync();
    var settings = await context.UserInterface.SingleAsync();
    Assert.Equal("en", settings.Language);
    Assert.True(settings.UseTopMenuIcons);
    Assert.Equal(0, settings.AutoLockMinutes);
    Assert.Null(settings.AdministratorAutoLockMinutes);
    Assert.Null(settings.DeveloperAutoLockMinutes);
    Assert.Null(settings.AdjusterAutoLockMinutes);
    Assert.Null(settings.RootAutoLockMinutes);
    settings.AutoLockMinutes = 5;
    await context.SaveChangesAsync();
    context.ChangeTracker.Clear();
    Assert.Equal(5, (await context.UserInterface.SingleAsync()).AutoLockMinutes);
  }

  [Fact]
  public async Task FreshDatabaseStoresAutoLock()
  {
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
    await context.Database.MigrateAsync();
    context.UserInterface.Add(new UserInterfaceDto
    {
      Language = "ru", AutoLockMinutes = 10,
      AdministratorAutoLockMinutes = 0, DeveloperAutoLockMinutes = 2,
      AdjusterAutoLockMinutes = 5, RootAutoLockMinutes = 10
    });
    await context.SaveChangesAsync();
    context.ChangeTracker.Clear();
    var settings = await context.UserInterface.SingleAsync();
    Assert.Equal(10, settings.AutoLockMinutes);
    Assert.Equal(0, settings.AdministratorAutoLockMinutes);
    Assert.Equal(2, settings.DeveloperAutoLockMinutes);
    Assert.Equal(5, settings.AdjusterAutoLockMinutes);
    Assert.Equal(10, settings.RootAutoLockMinutes);
  }

  [Fact]
  public async Task AdoptedSchemaRepairIsIdempotentAndPreservesSavedTimeout()
  {
    var path = Path.Combine(Path.GetTempPath(), $"Ask-autolock-{Guid.NewGuid():N}.db");
    try
    {
      await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
      await connection.OpenAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = "CREATE TABLE UserInterface (Id INTEGER PRIMARY KEY, Language TEXT); INSERT INTO UserInterface VALUES (1, 'en');";
      await command.ExecuteNonQueryAsync();
      var report = new DatabaseInitializationReport();
      await DatabaseInitializationService.EnsureUserInterfaceAutoLockColumnAsync(path, report, null, CancellationToken.None);
      command.CommandText = "SELECT AutoLockMinutes FROM UserInterface";
      Assert.Equal(0L, await command.ExecuteScalarAsync());
      command.CommandText = "UPDATE UserInterface SET AutoLockMinutes=2, AdministratorAutoLockMinutes=0, DeveloperAutoLockMinutes=10, AdjusterAutoLockMinutes=5, RootAutoLockMinutes=1";
      await command.ExecuteNonQueryAsync();
      await DatabaseInitializationService.EnsureUserInterfaceAutoLockColumnAsync(path, report, null, CancellationToken.None);
      command.CommandText = "SELECT AutoLockMinutes FROM UserInterface";
      Assert.Equal(2L, await command.ExecuteScalarAsync());
      foreach (var (column, expected) in new[] { ("AdministratorAutoLockMinutes", 0L), ("DeveloperAutoLockMinutes", 10L), ("AdjusterAutoLockMinutes", 5L), ("RootAutoLockMinutes", 1L) })
      {
        command.CommandText = $"SELECT {column} FROM UserInterface";
        Assert.Equal(expected, await command.ExecuteScalarAsync());
      }
      command.CommandText = "SELECT Language FROM UserInterface";
      Assert.Equal("en", await command.ExecuteScalarAsync());
    }
    finally
    {
      SqliteConnection.ClearAllPools();
      File.Delete(path);
    }
  }
}
