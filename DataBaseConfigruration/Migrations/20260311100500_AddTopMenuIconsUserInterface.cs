using DataBaseConfiguration.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  [DbContext(typeof(AppDbContext))]
  [Migration("20260311100500_AddTopMenuIconsUserInterface")]
  public partial class AddTopMenuIconsUserInterface : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<bool>(
          name: "UseTopMenuIcons",
          table: "UserInterface",
          type: "INTEGER",
          nullable: false,
          defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "UseTopMenuIcons",
          table: "UserInterface");
    }
  }
}
