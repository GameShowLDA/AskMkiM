using Ask.DataBase.Provider.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ask.DataBase.Provider.Migrations;

/// <summary>
/// Связывает внешние источники питания с выбранным тестером.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261029100000_AddPowerSupplyChassis")]
public partial class AddPowerSupplyChassis : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
    => migrationBuilder.AddColumn<int>("NumberChassis", "PowerSupplies", type: "INTEGER", nullable: false, defaultValue: 0);

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
    => migrationBuilder.DropColumn("NumberChassis", "PowerSupplies");
}
