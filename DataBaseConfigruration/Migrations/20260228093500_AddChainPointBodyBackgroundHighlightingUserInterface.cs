using DataBaseConfiguration.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  [DbContext(typeof(AppDbContext))]
  [Migration("20260228093500_AddChainPointBodyBackgroundHighlightingUserInterface")]
  public partial class AddChainPointBodyBackgroundHighlightingUserInterface : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<bool>(
          name: "UseChainPointBodyBackgroundHighlighting",
          table: "UserInterface",
          type: "INTEGER",
          nullable: false,
          defaultValue: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "UseChainPointBodyBackgroundHighlighting",
          table: "UserInterface");
    }
  }
}
