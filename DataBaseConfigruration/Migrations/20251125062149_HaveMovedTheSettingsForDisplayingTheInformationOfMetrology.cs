using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  public partial class HaveMovedTheSettingsForDisplayingTheInformationOfMetrology : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "ShowErrorResultMetrology",
          table: "DeviceDisplaySettings");

      migrationBuilder.DropColumn(
          name: "ShowRangeMetrology",
          table: "DeviceDisplaySettings");

      migrationBuilder.AddColumn<bool>(
          name: "ShowErrorResultMetrology",
          table: "SettingsProtocol",
          type: "INTEGER",
          nullable: false,
          defaultValue: false);

      migrationBuilder.AddColumn<bool>(
          name: "ShowRangeMetrology",
          table: "SettingsProtocol",
          type: "INTEGER",
          nullable: false,
          defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "ShowErrorResultMetrology",
          table: "SettingsProtocol");

      migrationBuilder.DropColumn(
          name: "ShowRangeMetrology",
          table: "SettingsProtocol");

      migrationBuilder.AddColumn<bool>(
          name: "ShowErrorResultMetrology",
          table: "DeviceDisplaySettings",
          type: "INTEGER",
          nullable: false,
          defaultValue: false);

      migrationBuilder.AddColumn<bool>(
          name: "ShowRangeMetrology",
          table: "DeviceDisplaySettings",
          type: "INTEGER",
          nullable: false,
          defaultValue: false);
    }
  }
}
