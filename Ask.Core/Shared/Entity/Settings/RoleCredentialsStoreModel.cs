using Ask.Core.Shared.Metadata.Enums.RoleEnums;

namespace Ask.Core.Shared.Entity.Settings
{
  /// <summary>
  /// Модель локального файла авторизации ролей.
  /// </summary>
  public sealed class RoleCredentialsStoreModel
  {
    /// <summary>
    /// Последняя успешно выбранная роль.
    /// </summary>
    public RoleType? LastSelectedRole { get; set; }

    /// <summary>
    /// Список доступных ролей для входа.
    /// </summary>
    public List<RoleCredentialModel> Roles { get; set; } = new();
  }
}
