using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
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
      SetAdminConsoleEnabled(true);
      SetTestsMenuVisible(true);
    }

    /// <summary>
    /// Применяет настройки, доступные для роли регулировщика.
    /// </summary>
    private static void ApplyAdjusterRole()
    {
      SetAdminConsoleEnabled(false);
      SetTestsMenuVisible(true);
    }

    /// <summary>
    /// Применяет настройки, доступные для роли разработчика.
    /// </summary>
    private static void ApplyDeveloperRole()
    {
      SetAdminConsoleEnabled(false);
      SetTestsMenuVisible(false);
    }

    /// <summary>
    /// Включает или отключает доступ к консоли администратора.
    /// </summary>
    /// <param name="enabled">Значение true включает консоль администратора, false отключает и скрывает её.</param>
    public static void SetAdminConsoleEnabled(bool enabled)
    {
      SystemStateEventAdapter.RaiseConsoleAccessChanged(enabled);
    }

    /// <summary>
    /// Включает или отключает отображение меню испытаний.
    /// </summary>
    /// <param name="visible">Значение true показывает меню испытаний, false скрывает его.</param>
    public static void SetTestsMenuVisible(bool visible)
    {
      SystemStateEventAdapter.RaiseTestsMenuVisibilityChanged(visible);
    }
  }
}
