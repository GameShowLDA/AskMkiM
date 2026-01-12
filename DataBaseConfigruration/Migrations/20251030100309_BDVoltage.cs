using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  public partial class BDVoltage : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<int>(
          name: "IRMinVoltage",
          table: "BreakdownTesters",
          type: "INTEGER",
          nullable: false,
          defaultValue: 0);

      migrationBuilder.AddColumn<int>(
          name: "MaxVoltage",
          table: "BreakdownTesters",
          type: "INTEGER",
          nullable: false,
          defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "IRMinVoltage",
          table: "BreakdownTesters");

      migrationBuilder.DropColumn(
          name: "MaxVoltage",
          table: "BreakdownTesters");
    }
  }
}
