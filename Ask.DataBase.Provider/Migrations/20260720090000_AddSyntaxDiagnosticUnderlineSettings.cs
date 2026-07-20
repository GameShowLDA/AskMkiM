using Ask.DataBase.Provider.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260720090000_AddSyntaxDiagnosticUnderlineSettings")]
    public partial class AddSyntaxDiagnosticUnderlineSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseWarningUnderlineHighlighting",
                table: "UserInterface",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseErrorUnderlineHighlighting",
                table: "UserInterface",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseWarningUnderlineHighlighting",
                table: "UserInterface");

            migrationBuilder.DropColumn(
                name: "UseErrorUnderlineHighlighting",
                table: "UserInterface");
        }
    }
}
