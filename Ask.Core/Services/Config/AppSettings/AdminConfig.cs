using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;

namespace Ask.Core.Services.Config.AppSettings
{
  static public class AdminConfig
  {
    static AdminConfig()
    {
      EventAggregator.Subscribe<SystemStateEvents.AdminRightsChanged>(e => IsAdmin = e.IsAdmin);
    }

    /// <summary>
    /// Флаг, указывающий, запущено ли приложение с правами администратора.
    /// </summary>
    static internal bool IsAdmin { get; set; }

    /// <summary>
    /// Асинхронно устанавливает статус прав администратора и уведомляет систему.
    /// </summary>
    /// <param name="enable">
    /// <see langword="true"/>, если запущено с правами администратора;
    /// <see langword="false"/> — если в обычном режиме.
    /// </param>
    public static void SetAdminRights(bool enable) => SystemStateEventAdapter.RaiseAdminRightsChanged(enable);

    /// <summary>
    /// Асинхронно возвращает текущий статус прав администратора.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если приложение работает с правами администратора;
    /// <see langword="false"/> — если без них.
    /// </returns>
    public static bool GetAdminRights() => IsAdmin;

  }
}
