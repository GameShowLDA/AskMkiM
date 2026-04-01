using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using DataBaseConfiguration.Context;

namespace DataBaseConfiguration.Services.Settings
{
  /// <summary>
  /// Выполняет инициализацию таблицы паролей ролей значениями по умолчанию.
  /// </summary>
  internal static class RolePasswordSeeder
  {
    private const string DefaultPassword = "test";

    /// <summary>
    /// Создаёт записи для системных ролей, если они отсутствуют.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public static void Seed(AppDbContext context)
    {
      var defaultRoles = new[]
      {
        RoleType.Administrator,
        RoleType.Metrology,
        RoleType.SystemMaintenance,
        RoleType.Developer,
      };

      foreach (var role in defaultRoles)
      {
        bool exists = context.RolePasswords.Any(x => x.Role == role);
        if (!exists)
        {
          context.RolePasswords.Add(new RolePasswordModel
          {
            Role = role,
            Password = DefaultPassword,
          });
        }
      }

      context.SaveChanges();
    }
  }
}
