using Ask.DataBase.Provider.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Ask.Engine.UnitTests.DataBase;

public sealed class SyntaxDiagnosticUnderlineMigrationTests
{
  private const string PreviousMigration =
    "20260716040942_AddBreakdownTesterSystemInsulationResistance";

  [Fact]
  public async Task MigrateAsync_OldUserInterfaceSchema_AddsDisabledUnderlineSettings()
  {
    string databasePath = Path.Combine(
      Path.GetTempPath(),
      $"askmkim-old-ui-{Guid.NewGuid():N}.db");

    try
    {
      var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={databasePath}")
        .Options;

      await using var context = new AppDbContext(options);
      await context.Database.MigrateAsync(PreviousMigration);

      var oldColumns = await GetUserInterfaceColumnsAsync(databasePath);
      Assert.DoesNotContain("UseWarningUnderlineHighlighting", oldColumns);
      Assert.DoesNotContain("UseErrorUnderlineHighlighting", oldColumns);

      await context.Database.ExecuteSqlRawAsync(
        """
        INSERT INTO "UserInterface" (
          "Language",
          "Theme",
          "UseSyntaxHighlighting",
          "UseCommandBodyBackgroundHighlighting",
          "UseChainPointBodyBackgroundHighlighting",
          "UseTopMenuIcons",
          "UseCommandAutoCollapse")
        VALUES ('ru', 0, 1, 1, 1, 0, 0);
        """);

      await context.Database.MigrateAsync();
      context.ChangeTracker.Clear();

      var currentColumns = await GetUserInterfaceColumnsAsync(databasePath);
      Assert.Contains("UseWarningUnderlineHighlighting", currentColumns);
      Assert.Contains("UseErrorUnderlineHighlighting", currentColumns);

      var settings = await context.UserInterface.SingleAsync();
      Assert.True(settings.UseSyntaxHighlighting);
      Assert.True(settings.UseCommandBodyBackgroundHighlighting);
      Assert.True(settings.UseChainPointBodyBackgroundHighlighting);
      Assert.False(settings.UseWarningUnderlineHighlighting);
      Assert.False(settings.UseErrorUnderlineHighlighting);
    }
    finally
    {
      SqliteConnection.ClearAllPools();
      if (File.Exists(databasePath))
      {
        File.Delete(databasePath);
      }
    }
  }

  private static async Task<HashSet<string>> GetUserInterfaceColumnsAsync(string databasePath)
  {
    var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = "PRAGMA table_info(\"UserInterface\");";

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
      columns.Add(reader.GetString(1));
    }

    return columns;
  }
}
