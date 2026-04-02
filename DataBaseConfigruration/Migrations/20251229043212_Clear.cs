using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  public partial class Clear : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "PrecisionMeters");

      migrationBuilder.DropTable(
          name: "UserArchiveRootEntities");

      migrationBuilder.DropTable(
          name: "UserSessions");

      migrationBuilder.DropColumn(
          name: "Scope",
          table: "FileHotkeys");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<int>(
          name: "Scope",
          table: "FileHotkeys",
          type: "INTEGER",
          nullable: false,
          defaultValue: 0);

      migrationBuilder.CreateTable(
          name: "PrecisionMeters",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false),
            DeviceClass = table.Column<string>(type: "TEXT", nullable: false),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            Number = table.Column<int>(type: "INTEGER", nullable: false),
            NumberChassis = table.Column<int>(type: "INTEGER", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_PrecisionMeters", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "UserArchiveRootEntities",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            Description = table.Column<string>(type: "TEXT", nullable: true),
            DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            FolderPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
            IsEncryptionPlanned = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
            SearchRecursively = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_UserArchiveRootEntities", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "UserSessions",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            JsonData = table.Column<string>(type: "TEXT", nullable: false),
            SavedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_UserSessions", x => x.Id);
          });

      migrationBuilder.CreateIndex(
          name: "IX_UserArchiveRootEntities_FolderPath",
          table: "UserArchiveRootEntities",
          column: "FolderPath",
          unique: true);
    }
  }
}
