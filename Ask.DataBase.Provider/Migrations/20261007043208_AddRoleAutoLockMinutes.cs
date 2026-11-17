using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAutoLockMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdjusterAutoLockMinutes",
                table: "UserInterface",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdministratorAutoLockMinutes",
                table: "UserInterface",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeveloperAutoLockMinutes",
                table: "UserInterface",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RootAutoLockMinutes",
                table: "UserInterface",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjusterAutoLockMinutes",
                table: "UserInterface");

            migrationBuilder.DropColumn(
                name: "AdministratorAutoLockMinutes",
                table: "UserInterface");

            migrationBuilder.DropColumn(
                name: "DeveloperAutoLockMinutes",
                table: "UserInterface");

            migrationBuilder.DropColumn(
                name: "RootAutoLockMinutes",
                table: "UserInterface");
        }
    }
}
