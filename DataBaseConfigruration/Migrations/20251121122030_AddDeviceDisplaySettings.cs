using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  public partial class AddDeviceDisplaySettings : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "DeviceDisplaySettings",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            ShowMachineAddresses = table.Column<bool>(type: "INTEGER", nullable: false),
            ShowConnectionInfo = table.Column<bool>(type: "INTEGER", nullable: false),
            ShowDeviceExecutionParameters = table.Column<bool>(type: "INTEGER", nullable: false),
            ShowMeasurementResults = table.Column<bool>(type: "INTEGER", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_DeviceDisplaySettings", x => x.Id);
          });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "DeviceDisplaySettings");
    }
  }
}
