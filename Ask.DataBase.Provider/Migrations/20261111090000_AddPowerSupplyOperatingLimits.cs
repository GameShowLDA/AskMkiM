using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations;

/// <summary>
/// Добавляет настраиваемые диапазоны и шаги уставок внешних источников питания.
/// </summary>
[DbContext(typeof(Context.AppDbContext))]
[Migration("20261111090000_AddPowerSupplyOperatingLimits")]
public partial class AddPowerSupplyOperatingLimits : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.AddColumn<decimal>("MinVoltage", "PowerSupplies", type: "TEXT", nullable: false, defaultValue: 0m);
    migrationBuilder.AddColumn<decimal>("MaxVoltage", "PowerSupplies", type: "TEXT", nullable: false, defaultValue: 150m);
    migrationBuilder.AddColumn<decimal>("VoltageStep", "PowerSupplies", type: "TEXT", nullable: false, defaultValue: 0.1m);
    migrationBuilder.AddColumn<decimal>("MinCurrent", "PowerSupplies", type: "TEXT", nullable: false, defaultValue: 0m);
    migrationBuilder.AddColumn<decimal>("MaxCurrent", "PowerSupplies", type: "TEXT", nullable: false, defaultValue: 5m);
    migrationBuilder.AddColumn<decimal>("CurrentStep", "PowerSupplies", type: "TEXT", nullable: false, defaultValue: 0.001m);
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.DropColumn("MinVoltage", "PowerSupplies");
    migrationBuilder.DropColumn("MaxVoltage", "PowerSupplies");
    migrationBuilder.DropColumn("VoltageStep", "PowerSupplies");
    migrationBuilder.DropColumn("MinCurrent", "PowerSupplies");
    migrationBuilder.DropColumn("MaxCurrent", "PowerSupplies");
    migrationBuilder.DropColumn("CurrentStep", "PowerSupplies");
  }
}
