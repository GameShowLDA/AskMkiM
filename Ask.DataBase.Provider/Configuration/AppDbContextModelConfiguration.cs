using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.Core.Shared.DTO.Settings;
using Microsoft.EntityFrameworkCore;

namespace Ask.DataBase.Provider.Context;

/// <summary>
/// Конфигурация EF-модели для контекста базы данных.
/// Нужна для явной привязки DTO к именам таблиц и ключам,
/// особенно для settings DTO, где ключ задаётся как shadow property.
/// Файл лежит в папке <c>Configuration</c>, но остаётся частью partial-класса <see cref="AppDbContext"/>.
/// </summary>
public partial class AppDbContext
{
  /// <summary>
  /// Настраивает отображение DTO на таблицы базы данных.
  /// </summary>
  /// <param name="modelBuilder">Построитель модели EF Core.</param>
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<ChassisManagerDto>(entity =>
    {
      entity.ToTable("ChassisManagers");
      entity.HasKey(x => x.Id);
    });

    modelBuilder.Entity<RelaySwitchModuleDto>(entity =>
    {
      entity.ToTable("RelaySwitchModules");
      entity.HasKey(x => x.Id);
    });

    modelBuilder.Entity<PowerSourceModuleDto>(entity =>
    {
      entity.ToTable("PowerSourceModules");
      entity.HasKey(x => x.Id);
    });

    modelBuilder.Entity<SwitchingDeviceDto>(entity =>
    {
      entity.ToTable("SwitchingDevices");
      entity.HasKey(x => x.Id);
    });

    modelBuilder.Entity<FastMeterDto>(entity =>
    {
      entity.ToTable("FastMeters");
      entity.HasKey(x => x.Id);
    });

    modelBuilder.Entity<BreakdownTesterDto>(entity =>
    {
      entity.ToTable("BreakdownTesters");
      entity.HasKey(x => x.Id);
    });

    modelBuilder.Entity<RackDto>(entity =>
    {
      entity.ToTable("Rack");
      entity.HasKey(x => x.Id);
    });

    modelBuilder.Entity<UninterruptiblePowerSupplyDto>(entity =>
    {
      entity.ToTable("UninterruptiblePowerSupplies");
      entity.HasKey(x => x.Id);
    });

    modelBuilder.Entity<SettingsProtocolDto>(entity =>
    {
      entity.ToTable("SettingsProtocol");
      entity.Property<int>("Id");
      entity.HasKey("Id");
    });

    modelBuilder.Entity<SettingsExecutionDto>(entity =>
    {
      entity.ToTable("Execution");
      entity.Property<int>("Id");
      entity.HasKey("Id");
    });

    modelBuilder.Entity<FileHotkeyDto>(entity =>
    {
      entity.ToTable("FileHotkeys");
      entity.Property<int>("Id");
      entity.HasKey("Id");
      entity.Property(x => x.ActionName).HasMaxLength(100);
      entity.Property(x => x.KeyCombination).HasMaxLength(50);
      entity.Property(x => x.Description).HasMaxLength(255);
    });

    modelBuilder.Entity<UserInterfaceDto>(entity =>
    {
      entity.ToTable("UserInterface");
      entity.Property<int>("Id");
      entity.HasKey("Id");
    });

    modelBuilder.Entity<DeviceDisplaySettingsDto>(entity =>
    {
      entity.ToTable("DeviceDisplaySettings");
      entity.Property<int>("Id");
      entity.HasKey("Id");
    });
  }
}
