using Ask.Core.Shared.Entity.Settings;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Services.Settings
{
  public class DeviceDisplayService
  {
    /// <summary>
    /// Сохраняет настройки отображения устройств в БД.
    /// Если строки ещё нет, создаёт новую.
    /// Если строка есть, обновляет её.
    /// </summary>
    public async Task SaveDeviceDisplayAsync(DeviceDisplaySettingsModel session)
    {
      using var db = DataBaseConfig.Context;

      var existing = await db.Set<DeviceDisplaySettingsModel>().FirstOrDefaultAsync();
      if (existing == null)
      {
        await db.Set<DeviceDisplaySettingsModel>().AddAsync(session);
      }
      else
      {
        // Обновляем все поля
        db.Entry(existing).CurrentValues.SetValues(session);
      }

      await db.SaveChangesAsync();
    }

    /// <summary>
    /// Возвращает сохранённые настройки отображения устройств.
    /// Если строки нет, возвращает null.
    /// </summary>
    public async Task<DeviceDisplaySettingsModel?> GetDeviceDisplayAsync()
    {
      using var db = DataBaseConfig.Context;
      return await db.Set<DeviceDisplaySettingsModel>().FirstOrDefaultAsync();
    }
  }
}
