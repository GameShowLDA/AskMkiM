using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  public partial class AddIrMinVoltage : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "MaxVoltage",
          table: "BreakdownTesters");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<int>(
          name: "MaxVoltage",
          table: "BreakdownTesters",
          type: "INTEGER",
          nullable: false,
          defaultValue: 0);
    }
  }
}
