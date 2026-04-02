using Ask.Core.Shared.Entity.Settings;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Services.Settings
{
  public class ExecutionService
  {
    /// <summary>
    /// Сохраняет настройки протокола в БД.
    /// Если строки ещё нет, создаёт новую.
    /// Если строка есть, обновляет её.
    /// </summary>
    public async Task SaveExecutionAsync(SettingsExecutionModel value)
    {
      using var db = DataBaseConfig.Context;

      var existing = await db.Set<SettingsExecutionModel>().FirstOrDefaultAsync();
      if (existing == null)
      {
        await db.Set<SettingsExecutionModel>().AddAsync(value);
      }
      else
      {
        db.Entry(existing).CurrentValues.SetValues(value);
      }

      await db.SaveChangesAsync();
    }

    /// <summary>
    /// Возвращает сохранённые настройки протокола.
    /// Если строки нет, возвращает null.
    /// </summary>
    public async Task<SettingsExecutionModel?> GetExecutionAsync()
    {
      using var db = DataBaseConfig.Context;
      return await db.Set<SettingsExecutionModel>().FirstOrDefaultAsync();
    }
  }
}
