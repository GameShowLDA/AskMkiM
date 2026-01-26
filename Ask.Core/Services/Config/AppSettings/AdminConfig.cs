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
      EventAggregator.Subscribe<SystemStateEvents.DebugRightsChanged>(e => IsDebug = e.IsDebug);
    }

    /// <summary>
    /// Флаг, указывающий, запущено ли приложение с правами администратора.
    /// </summary>
    static internal bool IsAdmin { get; set; }

    static internal bool IsDebug { get; set; }

    /// <summary>
    /// Флаг, указывающий перехват Exception в режиме админа.
    /// </summary>
    static public bool ErrorDebug { get; set; } = false;

    /// <summary>
    /// Асинхронно устанавливает статус прав администратора и уведомляет систему.
    /// </summary>
    /// <param name="enable">
    /// <see langword="true"/>, если запущено с правами администратора;
    /// <see langword="false"/> — если в обычном режиме.
    /// </param>
    public static async Task SetAdminRights(bool enable) =>
      await Task.Run(() => SystemStateEventAdapter.RaiseAdminRightsChanged(enable));

    /// <summary>
    /// Асинхронно возвращает текущий статус прав администратора.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если приложение работает с правами администратора;
    /// <see langword="false"/> — если без них.
    /// </returns>
    public static bool GetAdminRights() =>  IsAdmin;

    /// <summary>
    /// Асинхронно устанавливает статус прав администратора и уведомляет систему.
    /// </summary>
    /// <param name="enable">
    /// <see langword="true"/>, если запущено с правами администратора;
    /// <see langword="false"/> — если в обычном режиме.
    /// </param>
    public static async Task SetDebugRights(bool enable) =>
      await Task.Run(() => SystemStateEventAdapter.RaiseDebugRightsChanged(enable));

    /// <summary>
    /// Асинхронно возвращает текущий статус прав администратора.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если приложение работает с правами администратора;
    /// <see langword="false"/> — если без них.
    /// </returns>
    public static bool GetDebugRights() => IsDebug;
  }
}
