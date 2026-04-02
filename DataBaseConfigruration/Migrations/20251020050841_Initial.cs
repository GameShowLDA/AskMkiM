using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBaseConfiguration.Migrations
{
  /// <inheritdoc />
  public partial class Initial : Migration
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
            DeviceClass = table.Column<string>(type: "TEXT", nullable: false),
            MaxVoltage = table.Column<int>(type: "INTEGER", nullable: false)
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
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
            DeviceClass = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_ChassisManagers", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Execution",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            IdleModeExecution = table.Column<bool>(type: "INTEGER", nullable: false),
            IsErrorSimulationMode = table.Column<bool>(type: "INTEGER", nullable: false),
            StepByStepMode = table.Column<bool>(type: "INTEGER", nullable: false),
            StopOnError = table.Column<bool>(type: "INTEGER", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_Execution", x => x.Id);
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
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
            DeviceClass = table.Column<string>(type: "TEXT", nullable: false),
            MaxContinuityResistance = table.Column<int>(type: "INTEGER", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_FastMeters", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "FileHotkeys",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            ActionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
            KeyCombination = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
            IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
            Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
            Scope = table.Column<int>(type: "INTEGER", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_FileHotkeys", x => x.Id);
          });

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
          name: "PowerSourceModules",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            NumberChassis = table.Column<int>(type: "INTEGER", nullable: false),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false),
            Number = table.Column<int>(type: "INTEGER", nullable: false),
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
            DeviceClass = table.Column<string>(type: "TEXT", nullable: false),
            ResistanceCalibrationJson = table.Column<string>(type: "TEXT", nullable: true)
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
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
            DeviceClass = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_PrecisionMeters", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Rack",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false),
            Number = table.Column<int>(type: "INTEGER", nullable: false),
            NumberChassis = table.Column<int>(type: "INTEGER", nullable: false),
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
            DeviceClass = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_Rack", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "RelaySwitchModules",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            NumberChassis = table.Column<int>(type: "INTEGER", nullable: false),
            NumberRack = table.Column<int>(type: "INTEGER", nullable: false),
            PointCount = table.Column<int>(type: "INTEGER", nullable: false),
            Name = table.Column<string>(type: "TEXT", nullable: false),
            Description = table.Column<string>(type: "TEXT", nullable: false),
            Number = table.Column<int>(type: "INTEGER", nullable: false),
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
            DeviceClass = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_RelaySwitchModules", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "SettingsProtocol",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            ShowDeviceInfo = table.Column<bool>(type: "INTEGER", nullable: false),
            AutoSaveProtocol = table.Column<bool>(type: "INTEGER", nullable: false),
            AutoPrintProtocol = table.Column<bool>(type: "INTEGER", nullable: false),
            DisplayOperationTime = table.Column<bool>(type: "INTEGER", nullable: false),
            ShowDetailedProtocol = table.Column<bool>(type: "INTEGER", nullable: false),
            ShowProtocolInSoftware = table.Column<bool>(type: "INTEGER", nullable: false),
            GenerateProtocol = table.Column<bool>(type: "INTEGER", nullable: false),
            UseSyntaxHighlighting = table.Column<bool>(type: "INTEGER", nullable: false),
            CleanTextProtocol = table.Column<string>(type: "TEXT", nullable: false),
            CleanTextErrorsProtocol = table.Column<string>(type: "TEXT", nullable: false),
            ErrorTextProtocol = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_SettingsProtocol", x => x.Id);
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
            ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
            DeviceClass = table.Column<string>(type: "TEXT", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_SwitchingDevices", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "UserArchiveRootEntities",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
            FolderPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
            SearchRecursively = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
            IsEncryptionPlanned = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
            Description = table.Column<string>(type: "TEXT", nullable: true)
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

      migrationBuilder.CreateTable(
          name: "MeasurementErrorRanges",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            MinValue = table.Column<double>(type: "REAL", nullable: false),
            MaxValue = table.Column<double>(type: "REAL", nullable: true),
            NumericError = table.Column<double>(type: "REAL", nullable: false),
            PercentageError = table.Column<double>(type: "REAL", nullable: false),
            MeasurementErrorEntityId = table.Column<int>(type: "INTEGER", nullable: false)
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

      migrationBuilder.CreateIndex(
          name: "IX_UserArchiveRootEntities_FolderPath",
          table: "UserArchiveRootEntities",
          column: "FolderPath",
          unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "BreakdownTesters");

      migrationBuilder.DropTable(
          name: "ChassisManagers");

      migrationBuilder.DropTable(
          name: "Execution");

      migrationBuilder.DropTable(
          name: "FastMeters");

      migrationBuilder.DropTable(
          name: "FileHotkeys");

      migrationBuilder.DropTable(
          name: "MeasurementErrorRanges");

      migrationBuilder.DropTable(
          name: "PowerSourceModules");

      migrationBuilder.DropTable(
          name: "PrecisionMeters");

      migrationBuilder.DropTable(
          name: "Rack");

      migrationBuilder.DropTable(
          name: "RelaySwitchModules");

      migrationBuilder.DropTable(
          name: "SettingsProtocol");

      migrationBuilder.DropTable(
          name: "SwitchingDevices");

      migrationBuilder.DropTable(
          name: "UserArchiveRootEntities");

      migrationBuilder.DropTable(
          name: "UserSessions");

      migrationBuilder.DropTable(
          name: "MeasurementErrors");
    }
  }
}
