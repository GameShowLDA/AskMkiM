using Ask.Core.Shared.Entity.Settings;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Services.Settings
{
  public class ProtocolService
  {
    /// <summary>
    /// Сохраняет настройки протокола в БД.
    /// Если строки ещё нет, создаёт новую.
    /// Если строка есть, обновляет её.
    /// </summary>
    public async Task SaveProtocolAsync(SettingsProtocolModel session)
    {
      using var db = DataBaseConfig.Context;

      var existing = await db.Set<SettingsProtocolModel>().FirstOrDefaultAsync();
      if (existing == null)
      {
        await db.Set<SettingsProtocolModel>().AddAsync(session);
      }
      else
      {
        // Обновляем все поля
        db.Entry(existing).CurrentValues.SetValues(session);
      }

      await db.SaveChangesAsync();
    }

    /// <summary>
    /// Возвращает сохранённые настройки протокола.
    /// Если строки нет, возвращает null.
    /// </summary>
    public async Task<SettingsProtocolModel?> GetProtocolAsync()
    {
      using var db = DataBaseConfig.Context;
      return await db.Set<SettingsProtocolModel>().FirstOrDefaultAsync();
    }
  }
}
