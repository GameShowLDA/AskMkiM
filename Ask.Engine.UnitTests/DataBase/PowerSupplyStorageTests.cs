using Ask.Core.Shared.DTO.Devices.PowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;
using Ask.DataBase.Engine.Builder;
using Ask.DataBase.Engine.Services;
using Ask.DataBase.Provider.Context;
using Ask.DataBase.Provider.Initialization;
using Ask.DataBase.Provider.Services.Devices;
using Ask.Device.Runtime.Device;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Ask.Engine.UnitTests.DataBase;

public sealed class PowerSupplyStorageTests
{
  [Fact]
  public async Task DeviceEngine_PersistsAndRebuildsExternalSourceWithoutChassis()
  {
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
    await using (var context = new AppDbContext(options)) await context.Database.EnsureCreatedAsync();
    var cache = new DeviceCache();
    var engine = new DeviceEngine(cache: cache, powerSupplyService: new SupplyService(options));
    try
    {
      IPowerSupply device = new B5108
      {
        Name = "Лабораторный источник",
        Number = 7,
        NumberChassis = 1,
        ConnectionDetails = "192.0.2.17",
        IsHardwareFailureSimulationEnabled = true,
      };
      ((B5108)device).ConnectedProfile.Port = 5080;
      var created = await engine.CreateAsync(device);
      Assert.True(created.Id > 0);
      Assert.Equal("VoltageManagerAdapter", created.VoltageManager.GetType().Name);
      cache.Clear();
      var loaded = await engine.GetByIdAsync<IPowerSupply>(created.Id);
      var source = Assert.IsType<B5108>(loaded);
      Assert.Equal("Лабораторный источник", source.Name);
      Assert.Equal(7, source.Number);
      Assert.Equal(1, source.NumberChassis);
      Assert.Equal("192.0.2.17", source.ConnectionDetails);
      Assert.Equal(5080, source.ConnectedProfile.Port);
      Assert.True(source.IsHardwareFailureSimulationEnabled);
      Assert.Equal(0.1m, source.VoltageManager.VoltageStep);
      Assert.Equal(0.001m, source.CurrentManager.CurrentStep);
      Assert.False(source.ConnectionInfo.IsConnected);
      Assert.Single(await engine.GetAllAsync<IPowerSupply>());
      var other = await engine.CreateAsync<IPowerSupply>(new B5108 { Number = 7, NumberChassis = 2, ConnectionDetails = "192.0.2.27" });
      Assert.Equal(source.Id, Assert.Single(await engine.GetDevicesByNumberChassisAsync<IPowerSupply>(1)).Id);
      Assert.Equal(other.Id, Assert.Single(await engine.GetDevicesByNumberChassisAsync<IPowerSupply>(2)).Id);
      Assert.Equal(source.Id, (await engine.GetDeviceByNumberChassisAsync<IPowerSupply>(1, 7))!.Id);
      source.ConnectionDetails = "192.0.2.18";
      source.ConnectedProfile.Port = 80;
      var updated = await engine.UpdateAsync<IPowerSupply>(source);
      Assert.NotSame(source, updated);
      Assert.Equal("192.0.2.18", updated.ConnectionDetails);
      Assert.Equal(80, Assert.IsType<B5108>(updated).ConnectedProfile.Port);
      Assert.True(await engine.DeleteByIdAsync<IPowerSupply>(updated.Id));
      Assert.Empty(await engine.GetDevicesByNumberChassisAsync<IPowerSupply>(1));
      Assert.NotNull(await engine.GetByIdAsync<IPowerSupply>(other.Id));
      Assert.True(await engine.DeleteByIdAsync<IPowerSupply>(other.Id));
      Assert.Empty(await engine.GetAllAsync<IPowerSupply>());
    }
    finally { cache.Clear(); }
  }

  [Fact]
  public async Task CompatibilityTableCreation_IsIdempotentAndPreservesExistingRows()
  {
    string path = Path.GetTempFileName();
    try
    {
      await using (var connection = new SqliteConnection($"Data Source={path}"))
      {
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE ExistingDevice (Id INTEGER PRIMARY KEY, Name TEXT); INSERT INTO ExistingDevice VALUES (1, 'unchanged');";
        await command.ExecuteNonQueryAsync();
      }
      await DatabaseInitializationService.EnsurePowerSupplyStorageAsync(path);
      await DatabaseInitializationService.EnsurePowerSupplyStorageAsync(path);
      var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path}").Options;
      await using var context = new AppDbContext(options);
      context.PowerSupplies.Add(new PowerSupplyDto { Name = "Б5-108", Number = 1,
        ConnectionDetails = "192.0.2.17", DeviceClass = typeof(B5108).FullName! });
      await context.SaveChangesAsync();
      Assert.Equal(80, (await context.PowerSupplies.SingleAsync()).Port);
      await using var verification = new SqliteConnection($"Data Source={path}");
      await verification.OpenAsync();
      await using var query = verification.CreateCommand();
      query.CommandText = "SELECT Name FROM ExistingDevice WHERE Id=1";
      Assert.Equal("unchanged", await query.ExecuteScalarAsync());
    }
    finally { SqliteConnection.ClearAllPools(); File.Delete(path); }
  }

  [Fact]
  public async Task Migrations_CreatePowerSupplyTableInNewDatabase()
  {
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
    await using var context = new AppDbContext(options);
    await context.Database.MigrateAsync();
    Assert.Contains("20261029090000_AddExternalPowerSupplies", await context.Database.GetAppliedMigrationsAsync());
    Assert.Empty(await context.PowerSupplies.ToListAsync());
  }

  private sealed class SupplyService(DbContextOptions<AppDbContext> options) : PowerSupplyDtoService
  {
    protected override AppDbContext CreateContext() => new(options);
  }
}
