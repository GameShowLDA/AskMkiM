using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Services.Settings
{
  public class RolePasswordService
  {
    /// <summary>
    /// Возвращает список доступных ролей для входа.
    /// </summary>
    public async Task<IReadOnlyList<RolePasswordModel>> GetRolePasswordsAsync()
    {
      using var db = DataBaseConfig.Context;

      return await db.Set<RolePasswordModel>()
        .AsNoTracking()
        .OrderBy(x => x.Id)
        .ToListAsync();
    }

    /// <summary>
    /// Проверяет пароль выбранной роли.
    /// </summary>
    public async Task<RolePasswordModel?> AuthorizeAsync(RoleType role, string password)
    {
      using var db = DataBaseConfig.Context;

      return await db.Set<RolePasswordModel>()
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Role == role && x.Password == password);
    }
  }
}
