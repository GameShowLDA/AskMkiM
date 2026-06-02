using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations
{
    /// <inheritdoc />
    [Migration("20260602090000_AddPpuDividerCoefficientToFastMeters")]
    public partial class AddPpuDividerCoefficientToFastMeters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PpuDividerCoefficientPercent",
                table: "FastMeters",
                type: "REAL",
                nullable: false,
                defaultValue: 100d);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PpuDividerCoefficientPercent",
                table: "FastMeters");
        }
    }
}
