using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Ask.DataBase.Provider.Context
{
  public partial class AppDbContext
  {
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      var converter = new ValueConverter<VoltageRange, string>(
        range => JsonSerializer.Serialize(range, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<VoltageRange>(json, (JsonSerializerOptions?)null)!);
      var comparer = new ValueComparer<VoltageRange>(
        (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null)
          == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
        range => JsonSerializer.Serialize(range, (JsonSerializerOptions?)null).GetHashCode(),
        range => JsonSerializer.Deserialize<VoltageRange>(
          JsonSerializer.Serialize(range, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);

      foreach (var name in new[]
      {
        nameof(BreakdownTesterDto.AcwVoltageRange),
        nameof(BreakdownTesterDto.DcwVoltageRange),
        nameof(BreakdownTesterDto.IrVoltageRange),
      })
      {
        var property = modelBuilder.Entity<BreakdownTesterDto>().Property<VoltageRange>(name);
        property.HasConversion(converter).IsRequired();
        property.Metadata.SetValueComparer(comparer);
      }
    }

    /// <summary>
    /// Таблица менеджеров шасси.
    /// </summary>
    public DbSet<ChassisManagerDto> ChassisManagers { get; set; }

    /// <summary>
    /// Таблица legacy-конфигураций аппаратуры АСК-МКИ.
    /// </summary>
    public DbSet<LegacyMkiHardwareProfileDto> LegacyMkiHardwareProfiles { get; set; }

    /// <summary>
    /// Таблица модулей коммутации реле.
    /// </summary>
    public DbSet<RelaySwitchModuleDto> RelaySwitchModules { get; set; }

    /// <summary>
    /// Таблица модулей источников напряжения и тока.
    /// </summary>
    public DbSet<PowerSourceModuleDto> PowerSourceModules { get; set; }

    /// <summary>
    /// Таблица устройств коммутации.
    /// </summary>
    public DbSet<SwitchingDeviceDto> SwitchingDevices { get; set; }

    /// <summary>
    /// Таблица быстрых измерителей.
    /// </summary>
    public DbSet<FastMeterDto> FastMeters { get; set; }

    /// <summary>
    /// Таблица пробойных установок.
    /// </summary>
    public DbSet<BreakdownTesterDto> BreakdownTesters { get; set; }

    /// <summary>
    /// Таблица стоек.
    /// </summary>
    public DbSet<RackDto> Rack { get; set; }

    /// <summary>
    /// Таблица бесперебойников.
    /// </summary>
    public DbSet<UninterruptiblePowerSupplyDto> UninterruptiblePowerSupplies { get; set; }
  }
}
