using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppConfig.Migrations
{
  /// <inheritdoc />
  public partial class InitialCreate : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "BreakdownTesters",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            NumberChassis = table.Column<int>(type: "INTEGER", nullable: false),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false),
            Number = table.Column<int>(type: "INTEGER", nullable: false),
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
            ConnectionType = table.Column<int>(type: "INTEGER", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_BreakdownTesters", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ChassisManagers",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false),
            Number = table.Column<int>(type: "INTEGER", nullable: false),
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_ChassisManagers", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "FastMeters",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            NumberChassis = table.Column<int>(type: "INTEGER", nullable: false),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false),
            Number = table.Column<int>(type: "INTEGER", nullable: false),
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_FastMeters", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "PowerSourceModules",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            NumberChassis = table.Column<int>(type: "INTEGER", nullable: false),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false),
            Number = table.Column<int>(type: "INTEGER", nullable: false),
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_PowerSourceModules", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "PrecisionMeters",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            NumberChassis = table.Column<int>(type: "INTEGER", nullable: false),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false),
            Number = table.Column<int>(type: "INTEGER", nullable: false),
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_PrecisionMeters", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "RelaySwitchModules",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            NumberChassis = table.Column<int>(type: "INTEGER", nullable: false),
            PointCount = table.Column<int>(type: "INTEGER", nullable: false),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false),
            Number = table.Column<int>(type: "INTEGER", nullable: false),
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_RelaySwitchModules", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "SwitchingDevices",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            NumberChassis = table.Column<int>(type: "INTEGER", nullable: false),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false),
            Number = table.Column<int>(type: "INTEGER", nullable: false),
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_SwitchingDevices", x => x.Id);
          });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "BreakdownTesters");

      migrationBuilder.DropTable(
          name: "ChassisManagers");

      migrationBuilder.DropTable(
          name: "FastMeters");

      migrationBuilder.DropTable(
          name: "PowerSourceModules");

      migrationBuilder.DropTable(
          name: "PrecisionMeters");

      migrationBuilder.DropTable(
          name: "RelaySwitchModules");

      migrationBuilder.DropTable(
          name: "SwitchingDevices");
    }
  }
}
