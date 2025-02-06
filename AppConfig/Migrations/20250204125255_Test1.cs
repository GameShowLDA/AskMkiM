using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppConfig.Migrations
{
  /// <inheritdoc />
  public partial class Test1 : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "ConnectionType",
          table: "BreakdownTesters");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<int>(
          name: "ConnectionType",
          table: "BreakdownTesters",
          type: "INTEGER",
          nullable: false,
          defaultValue: 0);
    }
  }
}
