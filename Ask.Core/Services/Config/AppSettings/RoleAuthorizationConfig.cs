using Ask.Core.Shared.Metadata.Enums.RoleEnums;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Хранит текущую авторизованную роль в рамках сессии приложения.
  /// </summary>
  public static class RoleAuthorizationConfig
  {
    /// <summary>
    /// Текущая роль пользователя.
    /// </summary>
    public static RoleType? CurrentRole { get; private set; }

    /// <summary>
    /// Отображаемое имя текущей роли.
    /// </summary>
    public static string CurrentRoleDisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Устанавливает текущую роль сессии.
    /// </summary>
    public static void SetCurrentRole(RoleType role, string displayName)
    {
      CurrentRole = role;
      CurrentRoleDisplayName = displayName;
    }

    /// <summary>
    /// Сбрасывает информацию о текущей роли.
    /// </summary>
    public static void Clear()
    {
      CurrentRole = null;
      CurrentRoleDisplayName = string.Empty;
    }
  }
}
