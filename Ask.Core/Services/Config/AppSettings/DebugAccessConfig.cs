using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Определяет доступность отладочных функций для текущей авторизованной роли.
  /// </summary>
  public static class DebugAccessConfig
  {
    /// <summary>
    /// Признак доступности отладочных функций.
    /// </summary>
    public static bool IsDebugEnabled => RoleAuthorizationConfig.CurrentRole == RoleType.Root;

    internal static void NotifyCurrentRoleChanged(bool wasDebugEnabled)
    {
      if (wasDebugEnabled != IsDebugEnabled)
      {
        SystemStateEventAdapter.RaiseDebugRightsChanged(IsDebugEnabled);
      }
    }
  }
}
