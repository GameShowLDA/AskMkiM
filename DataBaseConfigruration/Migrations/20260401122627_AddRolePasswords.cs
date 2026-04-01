using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RolePasswords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePasswords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePasswords_Role",
                table: "RolePasswords",
                column: "Role",
                unique: true);

            migrationBuilder.InsertData(
                table: "RolePasswords",
                columns: new[] { "Id", "Role", "Password" },
                values: new object[,]
                {
                    { 1, 0, "test" },
                    { 2, 1, "test" },
                    { 3, 2, "test" },
                    { 4, 3, "test" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePasswords");
        }
    }
}
