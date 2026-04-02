using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using DataBaseConfiguration.Context;

namespace DataBaseConfiguration.Services.Settings
{
  /// <summary>
  /// Seeds default role passwords and keeps the role list in sync with current application roles.
  /// </summary>
  internal static class RolePasswordSeeder
  {
    private const string DefaultPassword = "test";

    private static readonly IReadOnlyDictionary<RoleType, string> DefaultRoleDisplayNames = new Dictionary<RoleType, string>
    {
      [RoleType.Administrator] = "Администратор",
      [RoleType.Adjuster] = "Регулировщик",
      [RoleType.Developer] = "Разработчик",
    };

    public static void Seed(AppDbContext context)
    {
      var validRoleValues = DefaultRoleDisplayNames.Keys
        .Select(role => (int)role)
        .ToHashSet();

      var obsoleteRoles = context.RolePasswords
        .Where(x => !validRoleValues.Contains((int)x.Role))
        .ToList();

      if (obsoleteRoles.Count > 0)
      {
        context.RolePasswords.RemoveRange(obsoleteRoles);
      }

      foreach (var role in DefaultRoleDisplayNames.Keys)
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
