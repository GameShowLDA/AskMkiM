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
                name: "AcwPpuDividerCoefficientPercent",
                table: "FastMeters",
                type: "REAL",
                nullable: false,
                defaultValue: 100d);

            migrationBuilder.AddColumn<double>(
                name: "DcwPpuDividerCoefficientPercent",
                table: "FastMeters",
                type: "REAL",
                nullable: false,
                defaultValue: 100d);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcwPpuDividerCoefficientPercent",
                table: "FastMeters");

            migrationBuilder.DropColumn(
                name: "DcwPpuDividerCoefficientPercent",
                table: "FastMeters");
        }
    }
}
