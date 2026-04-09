using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations
{
    /// <inheritdoc />
    public partial class AddShowTestStepMessagesInProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UseCommandHeadersInProtocol",
                table: "SettingsProtocol",
                newName: "ShowCommandHeadersInProtocol");

            migrationBuilder.AddColumn<bool>(
                name: "ShowTestStepMessagesInProtocol",
                table: "SettingsProtocol",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowTestStepMessagesInProtocol",
                table: "SettingsProtocol");

            migrationBuilder.RenameColumn(
                name: "ShowCommandHeadersInProtocol",
                table: "SettingsProtocol",
                newName: "UseCommandHeadersInProtocol");
        }
    }
}
