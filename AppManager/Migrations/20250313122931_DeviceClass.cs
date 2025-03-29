using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppManager.Migrations
{
  /// <inheritdoc />
  public partial class DeviceClass : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "DeviceClass",
          table: "SwitchingDevices",
          type: "TEXT",
          nullable: false,
          defaultValue: "");

      migrationBuilder.AddColumn<string>(
          name: "DeviceClass",
          table: "RelaySwitchModules",
          type: "TEXT",
          nullable: false,
          defaultValue: "");

      migrationBuilder.AddColumn<string>(
          name: "DeviceClass",
          table: "Rack",
          type: "TEXT",
          nullable: false,
          defaultValue: "");

      migrationBuilder.AddColumn<string>(
          name: "DeviceClass",
          table: "PrecisionMeters",
          type: "TEXT",
          nullable: false,
          defaultValue: "");

      migrationBuilder.AddColumn<string>(
          name: "DeviceClass",
          table: "PowerSourceModules",
          type: "TEXT",
          nullable: false,
          defaultValue: "");

      migrationBuilder.AddColumn<string>(
          name: "DeviceClass",
          table: "FastMeters",
          type: "TEXT",
          nullable: false,
          defaultValue: "");

      migrationBuilder.AddColumn<string>(
          name: "DeviceClass",
          table: "ChassisManagers",
          type: "TEXT",
          nullable: false,
          defaultValue: "");

      migrationBuilder.AddColumn<string>(
          name: "DeviceClass",
          table: "BreakdownTesters",
          type: "TEXT",
          nullable: false,
          defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "DeviceClass",
          table: "SwitchingDevices");

      migrationBuilder.DropColumn(
          name: "DeviceClass",
          table: "RelaySwitchModules");

      migrationBuilder.DropColumn(
          name: "DeviceClass",
          table: "Rack");

      migrationBuilder.DropColumn(
          name: "DeviceClass",
          table: "PrecisionMeters");

      migrationBuilder.DropColumn(
          name: "DeviceClass",
          table: "PowerSourceModules");

      migrationBuilder.DropColumn(
          name: "DeviceClass",
          table: "FastMeters");

      migrationBuilder.DropColumn(
          name: "DeviceClass",
          table: "ChassisManagers");

      migrationBuilder.DropColumn(
          name: "DeviceClass",
          table: "BreakdownTesters");
    }
  }
}
