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
    "ChassisManagers",
    "BreakdownTesters",
    "FastMeters",
    "RelaySwitchModules",
    "PowerSourceModules",
    "SwitchingDevices",
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

      migrationBuilder.Sql(
        $"""
        UPDATE "{table}"
        SET "IsHardwareFailureSimulationEnabled" = COALESCE(
          (SELECT "IsHardwareErrorSimulationMode" FROM "Execution" LIMIT 1),
          0);
        """);
    }

  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    string enabledPredicate = string.Join(
      " OR ",
      DeviceTables.Select(table =>
        $"EXISTS (SELECT 1 FROM \"{table}\" WHERE \"IsHardwareFailureSimulationEnabled\" = 1)"));

    migrationBuilder.Sql(
      $"""
      UPDATE "Execution"
      SET "IsHardwareErrorSimulationMode" = CASE WHEN {enabledPredicate} THEN 1 ELSE 0 END;
      """);

    foreach (string table in DeviceTables)
    {
      migrationBuilder.Sql(
        $"ALTER TABLE \"{table}\" DROP COLUMN \"IsHardwareFailureSimulationEnabled\";");
    }
  }
}
