using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePasswordDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "RolePasswords",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE RolePasswords SET DisplayName = 'Администратор' WHERE Role = 0;
                UPDATE RolePasswords SET DisplayName = 'Метрология' WHERE Role = 1;
                UPDATE RolePasswords SET DisplayName = 'Обслуживание системы' WHERE Role = 2;
                UPDATE RolePasswords SET DisplayName = 'Разработчик' WHERE Role = 3;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "RolePasswords");
        }
    }
}
