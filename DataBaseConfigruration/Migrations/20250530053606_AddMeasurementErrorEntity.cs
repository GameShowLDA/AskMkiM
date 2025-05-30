using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
    /// <inheritdoc />
    public partial class AddMeasurementErrorEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeasurementErrors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    PercentageError = table.Column<double>(type: "REAL", nullable: false),
                    NumericError = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementErrors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementErrors_Type",
                table: "MeasurementErrors",
                column: "Type",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeasurementErrors");
        }
    }
}
