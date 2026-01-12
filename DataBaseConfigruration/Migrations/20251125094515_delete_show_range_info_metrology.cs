using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  public partial class delete_show_range_info_metrology : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "ShowRangeMetrology",
          table: "SettingsProtocol");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<bool>(
          name: "ShowRangeMetrology",
          table: "SettingsProtocol",
          type: "INTEGER",
          nullable: false,
          defaultValue: false);
    }
  }
}
