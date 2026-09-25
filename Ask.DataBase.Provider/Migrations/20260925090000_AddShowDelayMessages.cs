using Ask.DataBase.Provider.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ask.DataBase.Provider.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260925090000_AddShowDelayMessages")]
public sealed class AddShowDelayMessages : Migration
{
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.AddColumn<bool>(
      name: "ShowDelayMessages",
      table: "DeviceDisplaySettings",
      type: "INTEGER",
      nullable: false,
      defaultValue: true);
  }

  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.DropColumn(
      name: "ShowDelayMessages",
      table: "DeviceDisplaySettings");
  }
}
