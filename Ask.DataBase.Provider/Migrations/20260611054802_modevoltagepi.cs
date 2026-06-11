using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations
{
    /// <inheritdoc />
    public partial class modevoltagepi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PiMaxVoltage",
                table: "BreakdownTesters",
                newName: "DcwMaxVoltage");

            migrationBuilder.AddColumn<int>(
                name: "AcwMaxVoltage",
                table: "BreakdownTesters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcwMaxVoltage",
                table: "BreakdownTesters");

            migrationBuilder.RenameColumn(
                name: "DcwMaxVoltage",
                table: "BreakdownTesters",
                newName: "PiMaxVoltage");
        }
    }
}
