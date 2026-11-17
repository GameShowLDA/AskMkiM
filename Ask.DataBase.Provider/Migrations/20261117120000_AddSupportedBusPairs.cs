using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations;

/// <inheritdoc />
[DbContext(typeof(Context.AppDbContext))]
[Migration("20261117120000_AddSupportedBusPairs")]
public partial class AddSupportedBusPairs : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    AddColumn(migrationBuilder, "BreakdownTesters", 1);
    AddColumn(migrationBuilder, "FastMeters", 15);
    AddColumn(migrationBuilder, "PowerSourceModules", 15);
    AddColumn(migrationBuilder, "PowerSupplies", 14);
    AddColumn(migrationBuilder, "Rack", 0);
    AddColumn(migrationBuilder, "RelaySwitchModules", 1);
    AddColumn(migrationBuilder, "SwitchingDevices", 15);
    AddColumn(migrationBuilder, "UninterruptiblePowerSupplies", 0);

    migrationBuilder.Sql(
      """
      UPDATE "RelaySwitchModules"
      SET "SupportedBusPairs" = CASE "BusType"
        WHEN 1 THEN 2
        WHEN 2 THEN 4
        WHEN 3 THEN 8
        ELSE 1
      END;
      """);
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    foreach (string table in new[]
    {
      "BreakdownTesters", "FastMeters", "PowerSourceModules", "PowerSupplies",
      "Rack", "RelaySwitchModules", "SwitchingDevices", "UninterruptiblePowerSupplies"
    })
    {
      migrationBuilder.DropColumn(name: "SupportedBusPairs", table: table);
    }
  }

  private static void AddColumn(MigrationBuilder migrationBuilder, string table, int defaultValue)
  {
    migrationBuilder.AddColumn<int>(
      name: "SupportedBusPairs",
      table: table,
      type: "INTEGER",
      nullable: false,
      defaultValue: defaultValue);
  }
}
