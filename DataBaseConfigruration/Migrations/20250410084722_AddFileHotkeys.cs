using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
    /// <inheritdoc />
    public partial class AddFileHotkeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileHotkeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KeyCombination = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileHotkeys", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileHotkeys");
        }
    }
}
