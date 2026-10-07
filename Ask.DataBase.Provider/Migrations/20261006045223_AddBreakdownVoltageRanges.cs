using Microsoft.EntityFrameworkCore.Migrations;

namespace Ask.DataBase.Provider.Migrations
{
  /// <inheritdoc />
  public partial class AddBreakdownVoltageRanges : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.Sql("""
        ALTER TABLE "BreakdownTesters" ADD COLUMN "AcwVoltageRange" TEXT NOT NULL DEFAULT '{}';
        ALTER TABLE "BreakdownTesters" ADD COLUMN "DcwVoltageRange" TEXT NOT NULL DEFAULT '{}';
        ALTER TABLE "BreakdownTesters" ADD COLUMN "IrVoltageRange" TEXT NOT NULL DEFAULT '{}';
        UPDATE "BreakdownTesters" SET
          "AcwVoltageRange" = json_object('MinVoltage', 50, 'MaxVoltage', CASE WHEN "AcwMaxVoltage" > 0 THEN "AcwMaxVoltage" ELSE 700 END, 'Step', 2, 'Exceptions', json('[]')),
          "DcwVoltageRange" = json_object('MinVoltage', 50, 'MaxVoltage', CASE WHEN "DcwMaxVoltage" > 0 THEN "DcwMaxVoltage" ELSE 1000 END, 'Step', 2, 'Exceptions', json('[]')),
          "IrVoltageRange" = json_object('MinVoltage', CASE WHEN "IRMinVoltage" > 0 THEN "IRMinVoltage" ELSE 50 END, 'MaxVoltage', CASE WHEN "SiMaxVoltage" > 0 THEN "SiMaxVoltage" ELSE 1000 END, 'Step', 50, 'Exceptions', json('[125]'));
        ALTER TABLE "BreakdownTesters" DROP COLUMN "AcwMaxVoltage";
        ALTER TABLE "BreakdownTesters" DROP COLUMN "DcwMaxVoltage";
        ALTER TABLE "BreakdownTesters" DROP COLUMN "IRMinVoltage";
        ALTER TABLE "BreakdownTesters" DROP COLUMN "SiMaxVoltage";
        """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.Sql("""
        ALTER TABLE "BreakdownTesters" ADD COLUMN "AcwMaxVoltage" INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE "BreakdownTesters" ADD COLUMN "DcwMaxVoltage" INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE "BreakdownTesters" ADD COLUMN "IRMinVoltage" INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE "BreakdownTesters" ADD COLUMN "SiMaxVoltage" INTEGER NOT NULL DEFAULT 0;
        UPDATE "BreakdownTesters" SET
          "AcwMaxVoltage" = json_extract("AcwVoltageRange", '$.MaxVoltage'),
          "DcwMaxVoltage" = json_extract("DcwVoltageRange", '$.MaxVoltage'),
          "IRMinVoltage" = json_extract("IrVoltageRange", '$.MinVoltage'),
          "SiMaxVoltage" = json_extract("IrVoltageRange", '$.MaxVoltage');
        ALTER TABLE "BreakdownTesters" DROP COLUMN "AcwVoltageRange";
        ALTER TABLE "BreakdownTesters" DROP COLUMN "DcwVoltageRange";
        ALTER TABLE "BreakdownTesters" DROP COLUMN "IrVoltageRange";
        """);
    }
  }
}
