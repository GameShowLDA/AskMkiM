using AppConfig.Config;
using AppConfig.DataBase;
using AppConfig.DataBase.Services;
using Microsoft.EntityFrameworkCore;
using static Utilities.LoggerUtility;

namespace AppConfig.Data.Device
{
  internal class DeviceSettingsManager
  {
    /// <summary>
    /// Считывает конфигурацию устройств из базы данных.
    /// </summary>
    /// <returns>Задача, завершающаяся после загрузки конфигурации.</returns>
    static public async Task ReadDeviceConfigAsync()
    {
      try
      {

        DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={FileLocations.ConfigFilePath}");
        using var dbContext = new AppDbContext(optionsBuilder.Options);
        var deviceConfig = new DeviceConfig
        {
          ChassisManagers = new ChassisManagerRepository(dbContext).GetAll(),
          RelaySwitchModules = new RelaySwitchModuleRepository(dbContext).GetAll(),
          PowerSourceModules = new PowerSourceModuleRepository(dbContext).GetAll(),
          SwitchingDevices = new SwitchingDeviceRepository(dbContext).GetAll(),
          PrecisionMeters = new PrecisionMeterRepository(dbContext).GetAll(),
          FastMeters = new FastMeterRepository(dbContext).GetAll(),
          BreakdownTesters = new BreakdownTesterRepository(dbContext).GetAll()
        };

        LogInformation("Конфигурация устройств успешно загружена.");
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при загрузке конфигурации устройств: {ex.Message}");
      }
    }
  }
}
