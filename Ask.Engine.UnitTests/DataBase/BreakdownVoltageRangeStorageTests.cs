using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.DataBase.Provider.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ask.Engine.UnitTests.DataBase;

public sealed class BreakdownVoltageRangeStorageTests
{
  [Fact]
  public async Task FreshDatabaseMigratesAndStoresDeviceRanges()
  {
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
    await using var context = new AppDbContext(options);
    await context.Database.MigrateAsync();
    using var device = new Ask.Device.Runtime.Device.GPT79904();
    context.BreakdownTesters.Add(device.Convert());
    await context.SaveChangesAsync();
    context.ChangeTracker.Clear();
    var dto = await context.BreakdownTesters.SingleAsync();
    Assert.Equal(700, dto.AcwVoltageRange.MaxVoltage);
    Assert.True(dto.IrVoltageRange.IsAllowed(125));
  }

  [Fact]
  public void MappingCopiesMutableRangesWithoutSharingExceptions()
  {
    using var device = new Ask.Device.Runtime.Device.GPT79904();
    var dto = Ask.DataBase.Engine.Mapping.Device.BreakdownTesterMapper.ToDto(device);
    dto.IrVoltageRange.Exceptions.Add(175);
    Assert.DoesNotContain(175, device.IrManger.VoltageRange.Exceptions);
    Ask.DataBase.Engine.Mapping.Device.BreakdownTesterMapper.ApplyDto(device, dto);
    dto.IrVoltageRange.Exceptions.Add(225);
    Assert.Contains(175, device.IrManger.VoltageRange.Exceptions);
    Assert.DoesNotContain(225, device.IrManger.VoltageRange.Exceptions);
  }

  [Fact]
  public async Task AdoptedSchemaRepairIsIdempotentAndPreservesValues()
  {
    string path = Path.Combine(Path.GetTempPath(), $"Ask-voltage-{Guid.NewGuid():N}.db");
    try
    {
      await using (var connection = new SqliteConnection($"Data Source={path}"))
      {
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
          CREATE TABLE BreakdownTesters (Id INTEGER PRIMARY KEY, AcwMaxVoltage INTEGER NOT NULL,
            DcwMaxVoltage INTEGER NOT NULL, IRMinVoltage INTEGER NOT NULL, SiMaxVoltage INTEGER NOT NULL);
          INSERT INTO BreakdownTesters VALUES (1, 650, 900, 100, 950);
          """;
        await command.ExecuteNonQueryAsync();
      }
      var report = new Ask.DataBase.Provider.Initialization.DatabaseInitializationReport();
      for (int i = 0; i < 2; i++)
        await Ask.DataBase.Provider.Initialization.DatabaseInitializationService.EnsureBreakdownTesterVoltageColumnsAsync(
          path, report, null, CancellationToken.None);
      await using var check = new SqliteConnection($"Data Source={path}");
      await check.OpenAsync();
      await using var query = check.CreateCommand();
      query.CommandText = "SELECT json_extract(AcwVoltageRange, '$.MaxVoltage'), json_extract(IrVoltageRange, '$.MinVoltage') FROM BreakdownTesters";
      await using (var reader = await query.ExecuteReaderAsync())
      {
        Assert.True(await reader.ReadAsync());
        Assert.Equal(650, reader.GetInt32(0));
        Assert.Equal(100, reader.GetInt32(1));
      }
      query.CommandText = "SELECT count(*) FROM pragma_table_info('BreakdownTesters') WHERE name IN ('AcwMaxVoltage', 'DcwMaxVoltage', 'IRMinVoltage', 'SiMaxVoltage')";
      Assert.Equal(0L, await query.ExecuteScalarAsync());
    }
    finally
    {
      SqliteConnection.ClearAllPools();
      File.Delete(path);
    }
  }

  [Fact]
  public async Task RangesAndInPlaceExceptionChangesRoundTrip()
  {
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
    await using var context = new AppDbContext(options);
    await context.Database.EnsureCreatedAsync();
    var dto = new BreakdownTesterDto
    {
      AcwVoltageRange = new() { MinVoltage = 50, MaxVoltage = 650, Step = 2 },
      DcwVoltageRange = new() { MinVoltage = 50, MaxVoltage = 900, Step = 2 },
      IrVoltageRange = new() { MinVoltage = 50, MaxVoltage = 1000, Step = 50, Exceptions = [125] },
    };
    context.BreakdownTesters.Add(dto);
    await context.SaveChangesAsync();
    context.ChangeTracker.Clear();
    var loaded = await context.BreakdownTesters.SingleAsync();
    Assert.Equal(650, loaded.AcwVoltageRange.MaxVoltage);
    Assert.Equal(900, loaded.DcwVoltageRange.MaxVoltage);
    Assert.Equal(new double[] { 125 }, loaded.IrVoltageRange.Exceptions);
    loaded.IrVoltageRange.Exceptions.Add(175);
    loaded.AcwVoltageRange.MaxVoltage = 600;
    await context.SaveChangesAsync();
    context.ChangeTracker.Clear();
    loaded = await context.BreakdownTesters.SingleAsync();
    Assert.Equal(600, loaded.AcwVoltageRange.MaxVoltage);
    Assert.Equal(new double[] { 125, 175 }, loaded.IrVoltageRange.Exceptions);
  }

  [Fact]
  public async Task MigrationPreservesLegacyBoundsAndUnrelatedColumnsAndCanRollBack()
  {
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
    await using var context = new AppDbContext(options);
    var migrations = context.Database.GetMigrations().ToArray();
    await context.Database.ExecuteSqlRawAsync("""
      CREATE TABLE BreakdownTesters (Id INTEGER PRIMARY KEY, AcwMaxVoltage INTEGER NOT NULL,
        DcwMaxVoltage INTEGER NOT NULL, IRMinVoltage INTEGER NOT NULL, SiMaxVoltage INTEGER NOT NULL, Note TEXT);
      INSERT INTO BreakdownTesters VALUES (1, 650, 900, 100, 950, 'preserved');
      INSERT INTO BreakdownTesters VALUES (2, 0, 0, 0, 0, 'defaults');
      CREATE TABLE __EFMigrationsHistory (MigrationId TEXT PRIMARY KEY, ProductVersion TEXT NOT NULL);
      """);
    foreach (string migration in migrations.SkipLast(1))
      await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO __EFMigrationsHistory VALUES ({migration}, {"9.0.4"})");
    await context.Database.MigrateAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT AcwVoltageRange, DcwVoltageRange, IrVoltageRange, Note FROM BreakdownTesters";
    await using (var reader = await command.ExecuteReaderAsync())
    {
      Assert.True(await reader.ReadAsync());
      var acw = System.Text.Json.JsonSerializer.Deserialize<Ask.Core.Shared.DTO.Devices.Breakdown.VoltageRange>(reader.GetString(0))!;
      var dcw = System.Text.Json.JsonSerializer.Deserialize<Ask.Core.Shared.DTO.Devices.Breakdown.VoltageRange>(reader.GetString(1))!;
      var ir = System.Text.Json.JsonSerializer.Deserialize<Ask.Core.Shared.DTO.Devices.Breakdown.VoltageRange>(reader.GetString(2))!;
      Assert.Equal(650, acw.MaxVoltage);
      Assert.Equal(2, acw.Step);
      Assert.Equal(900, dcw.MaxVoltage);
      Assert.Equal(100, ir.MinVoltage);
      Assert.Equal(950, ir.MaxVoltage);
      Assert.Equal(50, ir.Step);
      Assert.Equal(new double[] { 125 }, ir.Exceptions);
      Assert.Equal("preserved", reader.GetString(3));
    }
    command.CommandText = "SELECT json_extract(AcwVoltageRange, '$.MaxVoltage'), json_extract(IrVoltageRange, '$.MinVoltage'), json_extract(IrVoltageRange, '$.MaxVoltage') FROM BreakdownTesters WHERE Id = 2";
    await using (var reader = await command.ExecuteReaderAsync())
    {
      Assert.True(await reader.ReadAsync());
      Assert.Equal(700, reader.GetInt32(0));
      Assert.Equal(50, reader.GetInt32(1));
      Assert.Equal(1000, reader.GetInt32(2));
    }
    await context.GetService<IMigrator>().MigrateAsync(migrations[^2]);
    command.CommandText = "SELECT AcwMaxVoltage, DcwMaxVoltage, IRMinVoltage, SiMaxVoltage FROM BreakdownTesters";
    await using var rollbackReader = await command.ExecuteReaderAsync();
    Assert.True(await rollbackReader.ReadAsync());
    Assert.Equal(650, rollbackReader.GetInt32(0));
    Assert.Equal(900, rollbackReader.GetInt32(1));
    Assert.Equal(100, rollbackReader.GetInt32(2));
    Assert.Equal(950, rollbackReader.GetInt32(3));
  }
}
