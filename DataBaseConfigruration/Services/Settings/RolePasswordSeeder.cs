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
    private static readonly IReadOnlyDictionary<RoleType, string> DefaultRoleDisplayNames = new Dictionary<RoleType, string>
    {
      [RoleType.Administrator] = "Администратор",
      [RoleType.Metrology] = "Метрология",
      [RoleType.SystemMaintenance] = "Обслуживание системы",
      [RoleType.Developer] = "Разработчик",
    };

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
        var entity = context.RolePasswords.FirstOrDefault(x => x.Role == role);
        if (entity == null)
        {
          context.RolePasswords.Add(new RolePasswordModel
          {
            Role = role,
            DisplayName = DefaultRoleDisplayNames[role],
            Password = DefaultPassword,
          });

          continue;
        }

        if (!string.Equals(entity.DisplayName, DefaultRoleDisplayNames[role], StringComparison.Ordinal))
        {
          entity.DisplayName = DefaultRoleDisplayNames[role];
        }

        if (string.IsNullOrWhiteSpace(entity.Password))
        {
          entity.Password = DefaultPassword;
        }
      }

      context.SaveChanges();
    }
  }
}
