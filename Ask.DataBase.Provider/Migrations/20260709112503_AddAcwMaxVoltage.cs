using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations
{
    /// <inheritdoc />
    public partial class AddAcwMaxVoltage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegacyMkiHardwareProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NumberChassis = table.Column<int>(type: "INTEGER", nullable: false),
                    ProfileKind = table.Column<byte>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Nas = table.Column<byte>(type: "INTEGER", nullable: false),
                    DvAcp = table.Column<byte>(type: "INTEGER", nullable: false),
                    DvV7 = table.Column<byte>(type: "INTEGER", nullable: false),
                    EtGui4 = table.Column<byte>(type: "INTEGER", nullable: false),
                    SkIs = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SkBkBeg = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SkBkEnd = table.Column<byte[]>(type: "BLOB", nullable: false),
                    GuiType = table.Column<byte[]>(type: "BLOB", nullable: false),
                    GuiVoltStep = table.Column<byte[]>(type: "BLOB", nullable: false),
                    GuiAmperStep = table.Column<byte[]>(type: "BLOB", nullable: false),
                    GuiVoltMax = table.Column<byte[]>(type: "BLOB", nullable: false),
                    GuiAmperMax = table.Column<byte[]>(type: "BLOB", nullable: false),
                    KuGui4 = table.Column<byte>(type: "INTEGER", nullable: false),
                    IsRos = table.Column<byte>(type: "INTEGER", nullable: false),
                    GomCmt = table.Column<double>(type: "REAL", nullable: false),
                    TyPpu = table.Column<byte>(type: "INTEGER", nullable: false),
                    PkiUmax = table.Column<ushort>(type: "INTEGER", nullable: false),
                    AcpTmr = table.Column<byte>(type: "INTEGER", nullable: false),
                    NAcpMaMax = table.Column<byte>(type: "INTEGER", nullable: false),
                    IsPki = table.Column<byte>(type: "INTEGER", nullable: false),
                    Comx4Com1 = table.Column<byte>(type: "INTEGER", nullable: false),
                    BbSpr = table.Column<byte>(type: "INTEGER", nullable: false),
                    LcIs = table.Column<byte>(type: "INTEGER", nullable: false),
                    RbusBb = table.Column<double>(type: "REAL", nullable: false),
                    PkiExtMo = table.Column<byte>(type: "INTEGER", nullable: false),
                    AcpIs0_3V = table.Column<byte>(type: "INTEGER", nullable: false),
                    DivGatBk = table.Column<byte>(type: "INTEGER", nullable: false),
                    UmaxEk = table.Column<double>(type: "REAL", nullable: false),
                    EkFull = table.Column<byte>(type: "INTEGER", nullable: false),
                    UmaxSiEkFull = table.Column<ushort>(type: "INTEGER", nullable: false),
                    UmaxPiEkFull = table.Column<ushort>(type: "INTEGER", nullable: false),
                    CalcPgr = table.Column<byte>(type: "INTEGER", nullable: false),
                    HardwareConfigReserved = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Res1 = table.Column<byte>(type: "INTEGER", nullable: false),
                    IsTstUpki = table.Column<byte>(type: "INTEGER", nullable: false),
                    U220 = table.Column<ushort>(type: "INTEGER", nullable: false),
                    PkiAkomDiv = table.Column<byte[]>(type: "BLOB", nullable: false),
                    RwirAdc = table.Column<double>(type: "REAL", nullable: false),
                    PkiKomTst = table.Column<byte[]>(type: "BLOB", nullable: false),
                    PpuKmul = table.Column<double>(type: "REAL", nullable: false),
                    UacpR = table.Column<double>(type: "REAL", nullable: false),
                    Uv7R = table.Column<double>(type: "REAL", nullable: false),
                    Net = table.Column<byte>(type: "INTEGER", nullable: false),
                    BeepOff = table.Column<byte>(type: "INTEGER", nullable: false),
                    Meas2 = table.Column<byte>(type: "INTEGER", nullable: false),
                    RwirV7 = table.Column<double>(type: "REAL", nullable: false),
                    Rgui4 = table.Column<double>(type: "REAL", nullable: false),
                    DIGui4mA = table.Column<double>(type: "REAL", nullable: false),
                    KopAddr = table.Column<ushort>(type: "INTEGER", nullable: false),
                    KmulKi = table.Column<double>(type: "REAL", nullable: false),
                    LocErrSob = table.Column<byte>(type: "INTEGER", nullable: false),
                    ShortSsRt = table.Column<byte>(type: "INTEGER", nullable: false),
                    PkiAVolt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    UseWait = table.Column<byte>(type: "INTEGER", nullable: false),
                    ReioVm = table.Column<byte>(type: "INTEGER", nullable: false),
                    PortSku = table.Column<byte[]>(type: "BLOB", nullable: false),
                    PortVm = table.Column<byte[]>(type: "BLOB", nullable: false),
                    PortFs = table.Column<byte[]>(type: "BLOB", nullable: false),
                    QMeasC = table.Column<byte>(type: "INTEGER", nullable: false),
                    OutUpi = table.Column<byte>(type: "INTEGER", nullable: false),
                    MksAcpTmr = table.Column<ushort>(type: "INTEGER", nullable: false),
                    UsbAddrVm = table.Column<string>(type: "TEXT", nullable: false),
                    TdobTdo = table.Column<double>(type: "REAL", nullable: false),
                    TdobTi = table.Column<double>(type: "REAL", nullable: false),
                    CcorrectPf = table.Column<short>(type: "INTEGER", nullable: false),
                    ReioGui3 = table.Column<byte>(type: "INTEGER", nullable: false),
                    PortGui3 = table.Column<byte[]>(type: "BLOB", nullable: false),
                    HardwareAuxReserved = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SkPwr = table.Column<ushort>(type: "INTEGER", nullable: false),
                    BkBus = table.Column<ushort>(type: "INTEGER", nullable: false),
                    EkRk = table.Column<ushort>(type: "INTEGER", nullable: false),
                    PtEk = table.Column<ushort>(type: "INTEGER", nullable: false),
                    PtRk = table.Column<ushort>(type: "INTEGER", nullable: false),
                    EpPwr = table.Column<ushort>(type: "INTEGER", nullable: false),
                    KzSh = table.Column<ushort>(type: "INTEGER", nullable: false),
                    GuiPwr = table.Column<ushort>(type: "INTEGER", nullable: false),
                    Gui4Mod = table.Column<ushort>(type: "INTEGER", nullable: false),
                    GuiGat = table.Column<ushort>(type: "INTEGER", nullable: false),
                    V734Mod = table.Column<ushort>(type: "INTEGER", nullable: false),
                    V753Mod = table.Column<ushort>(type: "INTEGER", nullable: false),
                    V765Mod = table.Column<ushort>(type: "INTEGER", nullable: false),
                    V7Gat = table.Column<ushort>(type: "INTEGER", nullable: false),
                    AcpMod = table.Column<ushort>(type: "INTEGER", nullable: false),
                    AcpGat = table.Column<ushort>(type: "INTEGER", nullable: false),
                    PkiPwr = table.Column<ushort>(type: "INTEGER", nullable: false),
                    PkiMod = table.Column<ushort>(type: "INTEGER", nullable: false),
                    PpuPwr = table.Column<ushort>(type: "INTEGER", nullable: false),
                    PpuMod = table.Column<ushort>(type: "INTEGER", nullable: false),
                    KoPwr = table.Column<ushort>(type: "INTEGER", nullable: false),
                    EpBef = table.Column<ushort>(type: "INTEGER", nullable: false),
                    V7Bef = table.Column<ushort>(type: "INTEGER", nullable: false),
                    AcpBef = table.Column<ushort>(type: "INTEGER", nullable: false),
                    PkiBef = table.Column<ushort>(type: "INTEGER", nullable: false),
                    PpuBef = table.Column<ushort>(type: "INTEGER", nullable: false),
                    V753RunR = table.Column<ushort>(type: "INTEGER", nullable: false),
                    V753RunU = table.Column<ushort>(type: "INTEGER", nullable: false),
                    V753RunV = table.Column<ushort>(type: "INTEGER", nullable: false),
                    V765RunR = table.Column<ushort>(type: "INTEGER", nullable: false),
                    V765RunU = table.Column<ushort>(type: "INTEGER", nullable: false),
                    V765RunUv = table.Column<ushort>(type: "INTEGER", nullable: false),
                    Gui3Mod = table.Column<ushort>(type: "INTEGER", nullable: false),
                    Reserv1 = table.Column<byte[]>(type: "BLOB", nullable: false),
                    GuiRst = table.Column<ushort>(type: "INTEGER", nullable: false),
                    LcBef = table.Column<ushort>(type: "INTEGER", nullable: false),
                    PpuAftPusk = table.Column<ushort>(type: "INTEGER", nullable: false),
                    TMeasUppuMin = table.Column<ushort>(type: "INTEGER", nullable: false),
                    TimingReserved = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Password0 = table.Column<string>(type: "TEXT", nullable: false),
                    Password1 = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyMkiHardwareProfiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacyMkiHardwareProfiles");
        }
    }
}
