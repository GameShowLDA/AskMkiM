using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  public partial class delete_property_show_error_metrology : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "ShowErrorResultMetrology",
          table: "SettingsProtocol");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<bool>(
          name: "ShowErrorResultMetrology",
          table: "SettingsProtocol",
          type: "INTEGER",
          nullable: false,
          defaultValue: false);
    }
  }
}
