using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;

namespace MainWindowProgram.Init
{
  /// <summary>
  /// Применяет настройки приложения для авторизованной роли.
  /// </summary>
  internal static class RoleApplicationConfigurator
  {
    /// <summary>
    /// Сохраняет текущую роль в сессии и выполняет настройку интерфейса под эту роль.
    /// </summary>
    /// <param name="role">Авторизованная роль пользователя.</param>
    public static void Apply(RoleCredentialModel role)
    {
      ArgumentNullException.ThrowIfNull(role);

      RoleAuthorizationConfig.SetCurrentRole(role.Role, role.DisplayName);

      switch (role.Role)
      {
        case RoleType.Administrator:
          ApplyAdministratorRole();
          break;

        case RoleType.Adjuster:
          ApplyAdjusterRole();
          break;

        case RoleType.Developer:
          ApplyDeveloperRole();
          break;
      }
    }

    /// <summary>
    /// Применяет настройки, доступные для роли администратора.
    /// </summary>
    private static void ApplyAdministratorRole()
    {
    }

    /// <summary>
    /// Применяет настройки, доступные для роли регулировщика.
    /// </summary>
    private static void ApplyAdjusterRole()
    {
    }

    /// <summary>
    /// Применяет настройки, доступные для роли разработчика.
    /// </summary>
    private static void ApplyDeveloperRole()
    {
    }
  }
}
