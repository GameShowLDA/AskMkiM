using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeasurementErrorRanges");

            migrationBuilder.DropTable(
                name: "MeasurementErrors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeasurementErrors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementErrors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementErrorRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MeasurementErrorEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxValue = table.Column<double>(type: "REAL", nullable: true),
                    MinValue = table.Column<double>(type: "REAL", nullable: false),
                    NumericError = table.Column<double>(type: "REAL", nullable: false),
                    PercentageError = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementErrorRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasurementErrorRanges_MeasurementErrors_MeasurementErrorEntityId",
                        column: x => x.MeasurementErrorEntityId,
                        principalTable: "MeasurementErrors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementErrorRanges_MeasurementErrorEntityId_MinValue_MaxValue",
                table: "MeasurementErrorRanges",
                columns: new[] { "MeasurementErrorEntityId", "MinValue", "MaxValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementErrors_Type",
                table: "MeasurementErrors",
                column: "Type",
                unique: true);
        }
    }
}
