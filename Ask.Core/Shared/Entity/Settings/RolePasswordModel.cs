using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using System.ComponentModel.DataAnnotations;

namespace Ask.Core.Shared.Entity.Settings
{
  /// <summary>
  /// Пароль для системной роли.
  /// </summary>
  public class RolePasswordModel
  {
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Роль, для которой хранится пароль.
    /// </summary>
    public RoleType Role { get; set; }

    /// <summary>
    /// Отображаемое название роли на русском языке.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Пароль роли.
    /// </summary>
    public string Password { get; set; } = string.Empty;
  }
}
