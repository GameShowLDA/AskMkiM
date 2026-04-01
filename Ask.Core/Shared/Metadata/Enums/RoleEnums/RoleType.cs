namespace Ask.Core.Shared.Metadata.Enums.RoleEnums
{
  /// <summary>
  /// Роли пользователей в системе.
  /// Используются для разграничения доступа и хранения паролей ролей.
  /// </summary>
  public enum RoleType
  {
    /// <summary>
    /// Администратор.
    /// </summary>
    Administrator,

    /// <summary>
    /// Роль метрологии.
    /// </summary>
    Metrology,

    /// <summary>
    /// Роль обслуживания системы.
    /// </summary>
    SystemMaintenance,

    /// <summary>
    /// Разработчик программ контроля.
    /// </summary>
    Developer,
  }
}
