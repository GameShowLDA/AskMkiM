using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations;

/// <summary>
/// Добавляет номер ПИНТ в конфигурацию программируемого источника напряжения и тока.
/// </summary>
[DbContext(typeof(Context.AppDbContext))]
[Migration("20261116090000_AddPowerSupplyPintNumber")]
public partial class AddPowerSupplyPintNumber : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.AddColumn<int>(
      name: "PintNumber",
      table: "PowerSupplies",
      type: "INTEGER",
      nullable: false,
      defaultValue: 1);
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.DropColumn(
      name: "PintNumber",
      table: "PowerSupplies");
  }
}
