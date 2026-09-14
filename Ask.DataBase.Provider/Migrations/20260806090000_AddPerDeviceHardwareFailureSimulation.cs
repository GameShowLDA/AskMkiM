using Ask.DataBase.Provider.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260806090000_AddPerDeviceHardwareFailureSimulation")]
public partial class AddPerDeviceHardwareFailureSimulation : Migration
{
  private static readonly string[] DeviceTables =
  [
    "BreakdownTesters",
    "ChassisManagers",
    "FastMeters",
    "PowerSourceModules",
    "Rack",
    "RelaySwitchModules",
    "SwitchingDevices",
    "UninterruptiblePowerSupplies",
  ];

  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    foreach (string table in DeviceTables)
    {
      migrationBuilder.AddColumn<bool>(
        name: "IsHardwareFailureSimulationEnabled",
        table: table,
        type: "INTEGER",
        nullable: false,
        defaultValue: false);
    }
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    foreach (string table in DeviceTables)
    {
      migrationBuilder.DropColumn(
        name: "IsHardwareFailureSimulationEnabled",
        table: table);
    }
  }
}
