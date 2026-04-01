using DataBaseConfiguration.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  [DbContext(typeof(AppDbContext))]
  [Migration("20260228090000_AddCommandBodyBackgroundHighlightingUserInterface")]
  public partial class AddCommandBodyBackgroundHighlightingUserInterface : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<bool>(
          name: "UseCommandBodyBackgroundHighlighting",
          table: "UserInterface",
          type: "INTEGER",
          nullable: false,
          defaultValue: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "UseCommandBodyBackgroundHighlighting",
          table: "UserInterface");
    }
  }
}
