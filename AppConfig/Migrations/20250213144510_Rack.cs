using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppConfig.Migrations
{
    /// <inheritdoc />
    public partial class Rack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberRack",
                table: "RelaySwitchModules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberRack",
                table: "RelaySwitchModules");
        }
    }
}
