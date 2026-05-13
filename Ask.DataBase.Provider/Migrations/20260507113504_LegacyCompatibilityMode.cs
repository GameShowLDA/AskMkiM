using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations
{
    /// <inheritdoc />
    public partial class LegacyCompatibilityMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LegacyCompatibilityMode",
                table: "Execution",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegacyCompatibilityMode",
                table: "Execution");
        }
    }
}
