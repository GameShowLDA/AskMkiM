using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations
{
    /// <inheritdoc />
    public partial class InitialProvider : Migration
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
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    PiMaxVoltage = table.Column<int>(type: "INTEGER", nullable: false),
                    SiMaxVoltage = table.Column<int>(type: "INTEGER", nullable: false),
                    IRMinVoltage = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceClass = table.Column<string>(type: "TEXT", nullable: false),
                    NumberChassis = table.Column<int>(type: "INTEGER", nullable: false)
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
                    BusType = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceClass = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChassisManagers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceDisplaySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShowMachineAddresses = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowConnectionInfo = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowDeviceExecutionParameters = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowMeasurementResults = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowIntermediateMeasurementResults = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceDisplaySettings", x => x.Id);
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
                    TypeMode = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxContinuityResistance = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceClass = table.Column<string>(type: "TEXT", nullable: false),
                    NumberChassis = table.Column<int>(type: "INTEGER", nullable: false)
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
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileHotkeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PowerSourceModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ResistanceCalibrationJson = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceClass = table.Column<string>(type: "TEXT", nullable: false),
                    NumberChassis = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PowerSourceModules", x => x.Id);
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
                    ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceClass = table.Column<string>(type: "TEXT", nullable: false),
                    NumberChassis = table.Column<int>(type: "INTEGER", nullable: false)
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
                    NumberRack = table.Column<int>(type: "INTEGER", nullable: false),
                    PointCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BusType = table.Column<int>(type: "INTEGER", nullable: false),
                    SwitchResistance = table.Column<double>(type: "REAL", nullable: false),
                    SwitchCapacitance = table.Column<double>(type: "REAL", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceClass = table.Column<string>(type: "TEXT", nullable: false),
                    NumberChassis = table.Column<int>(type: "INTEGER", nullable: false)
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
                    ShowHeaderInfo = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoSaveProtocol = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoPrintProtocol = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOperationTime = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowDetailedProtocol = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowProtocolInSoftware = table.Column<bool>(type: "INTEGER", nullable: false),
                    GenerateProtocol = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceClass = table.Column<string>(type: "TEXT", nullable: false),
                    NumberChassis = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwitchingDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UninterruptiblePowerSupplies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LastResolvedDevicePath = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectionDetails = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceClass = table.Column<string>(type: "TEXT", nullable: false),
                    NumberChassis = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UninterruptiblePowerSupplies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserInterface",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    Theme = table.Column<int>(type: "INTEGER", nullable: false),
                    UseSyntaxHighlighting = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseCommandBodyBackgroundHighlighting = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseChainPointBodyBackgroundHighlighting = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseTopMenuIcons = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInterface", x => x.Id);
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
                name: "DeviceDisplaySettings");

            migrationBuilder.DropTable(
                name: "Execution");

            migrationBuilder.DropTable(
                name: "FastMeters");

            migrationBuilder.DropTable(
                name: "FileHotkeys");

            migrationBuilder.DropTable(
                name: "PowerSourceModules");

            migrationBuilder.DropTable(
                name: "Rack");

            migrationBuilder.DropTable(
                name: "RelaySwitchModules");

            migrationBuilder.DropTable(
                name: "SettingsProtocol");

            migrationBuilder.DropTable(
                name: "SwitchingDevices");

            migrationBuilder.DropTable(
                name: "UninterruptiblePowerSupplies");

            migrationBuilder.DropTable(
                name: "UserInterface");
        }
    }
}
