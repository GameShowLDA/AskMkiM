using Ask.Core.Shared.Metadata.Enums.RoleEnums;

namespace Ask.Core.Shared.Entity.Settings
{
  /// <summary>
  /// Учетные данные роли для локального файла авторизации.
  /// </summary>
  public sealed class RoleCredentialModel
  {
    /// <summary>
    /// Системная роль.
    /// </summary>
    public RoleType Role { get; set; }

    /// <summary>
    /// Отображаемое название роли.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Хэш пароля роли.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Соль для хэша пароля.
    /// </summary>
    public string PasswordSalt { get; set; } = string.Empty;
  }
}
