using Ask.DataBase.Provider.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260807090000_AddMeasurementErrorSimulationMode")]
public partial class AddMeasurementErrorSimulationMode : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.AddColumn<int>(
      name: "MeasurementErrorSimulationMode",
      table: "Execution",
      type: "INTEGER",
      nullable: false,
      defaultValue: 0);

    migrationBuilder.Sql(
      """
      UPDATE "Execution"
      SET "MeasurementErrorSimulationMode" = 1
      WHERE "IsErrorSimulationMode" = 1;
      """);
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.DropColumn(
      name: "MeasurementErrorSimulationMode",
      table: "Execution");
  }
}
