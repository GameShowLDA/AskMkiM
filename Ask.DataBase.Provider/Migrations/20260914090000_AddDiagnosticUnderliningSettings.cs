using Ask.DataBase.Provider.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ask.DataBase.Provider.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260914090000_AddDiagnosticUnderliningSettings")]
public sealed class AddDiagnosticUnderliningSettings : Migration
{
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.AddColumn<bool>(
      name: "UseSyntaxErrorUnderlining", table: "UserInterface", type: "INTEGER", nullable: false, defaultValue: true);
    migrationBuilder.AddColumn<bool>(
      name: "UseStyleErrorUnderlining", table: "UserInterface", type: "INTEGER", nullable: false, defaultValue: true);
  }

  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.DropColumn(name: "UseSyntaxErrorUnderlining", table: "UserInterface");
    migrationBuilder.DropColumn(name: "UseStyleErrorUnderlining", table: "UserInterface");
  }
}
