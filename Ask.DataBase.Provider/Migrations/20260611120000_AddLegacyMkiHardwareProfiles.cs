using Ask.DataBase.Provider.Services.Devices;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ask.DataBase.Provider.Migrations
{
    /// <inheritdoc />
    [Migration("20260611120000_AddLegacyMkiHardwareProfiles")]
    public partial class AddLegacyMkiHardwareProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(LegacyMkiHardwareProfileStorageSql.CreateTableSql);
            migrationBuilder.Sql(LegacyMkiHardwareProfileStorageSql.CreateIndexSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacyMkiHardwareProfiles");
        }
    }
}
