using Ask.DataBase.Provider.Context;
using Ask.DataBase.Provider.Services.Devices;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ask.DataBase.Provider.Migrations;

/// <summary>
/// Добавляет таблицу внешних источников питания.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261029090000_AddExternalPowerSupplies")]
public partial class AddExternalPowerSupplies : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(PowerSupplyStorageSql.CreateTableSql);

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("PowerSupplies");
}
