namespace Ask.DataBase.Provider.Services.Devices;

/// <summary>
/// Содержит совместимое создание таблицы внешних источников питания.
/// </summary>
internal static class PowerSupplyStorageSql
{
  internal const string CreateTableSql = """
    CREATE TABLE IF NOT EXISTS "PowerSupplies" (
      "Id" INTEGER NOT NULL CONSTRAINT "PK_PowerSupplies" PRIMARY KEY AUTOINCREMENT,
      "Name" TEXT NOT NULL,
      "Description" TEXT NOT NULL,
      "Number" INTEGER NOT NULL,
      "ConnectionDetails" TEXT NOT NULL,
      "DeviceType" INTEGER NOT NULL,
      "DeviceClass" TEXT NOT NULL,
      "IsHardwareFailureSimulationEnabled" INTEGER NOT NULL DEFAULT 0,
      "Port" INTEGER NOT NULL DEFAULT 80
    );
    """;
}
