using Ask.DataBase.Provider.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260723090000_AddHardwareErrorSimulationMode")]
public partial class AddHardwareErrorSimulationMode : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.AddColumn<bool>(
      name: "IsHardwareErrorSimulationMode",
      table: "Execution",
      type: "INTEGER",
      nullable: false,
      defaultValue: false);
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.DropColumn(
      name: "IsHardwareErrorSimulationMode",
      table: "Execution");
  }
}
