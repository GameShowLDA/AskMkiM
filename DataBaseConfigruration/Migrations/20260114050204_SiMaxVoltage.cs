using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  public partial class SiMaxVoltage : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.RenameColumn(
          name: "MaxVoltage",
          table: "BreakdownTesters",
          newName: "SiMaxVoltage");

      migrationBuilder.AddColumn<int>(
          name: "PiMaxVoltage",
          table: "BreakdownTesters",
          type: "INTEGER",
          nullable: false,
          defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "PiMaxVoltage",
          table: "BreakdownTesters");

      migrationBuilder.RenameColumn(
          name: "SiMaxVoltage",
          table: "BreakdownTesters",
          newName: "MaxVoltage");
    }
  }
}
