using System.Diagnostics;

namespace AppConfig.Config
{
  static public class SystemStateManager
  {
    #region Properties.

    /// <summary>
    /// Флаг, указывающий, запущено ли приложение с правами администратора.
    /// </summary>
    static internal bool IsAdmin { get; set; }

    /// <summary>
    /// Таймер для измерения времени выполнения операций.
    /// </summary>
    static public readonly Stopwatch _stopwatch = new Stopwatch();

    /// <summary>
    /// Флаг, указывающий, активна ли система питания.
    /// </summary>
    static internal bool IsActivePower { get; set; }

    /// <summary>
    /// Флаг, указывающий на блокировку программы.
    /// </summary>
    static internal bool IsLocked { get; set; }

    #endregion

    #region Set.

    /// <summary>
    /// Устанавливает статус прав администратора.
    /// </summary>
    /// <param name="enable">true, если запущено с правами администратора; false в противном случае.</param>
    static public async Task SetAdminRights(bool enable)
    {
      await Task.Run(() =>
      {
        EventAggregator.AdminRightsFlag = enable;
      });
    }

    /// <summary>
    /// Включает или выключает питание системы.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static async Task SetIsActivePower(bool enable)
    {
      await Task.Run(() =>
      {
        EventAggregator.PowerFlag = enable;
      });
    }

    /// <summary>
    /// Включает или отключает блокировку UI элементов.
    /// </summary>
    /// <param name="enable"></param>
    public static async Task SetIsLocked(bool enable)
    {
      await Task.Run(() =>
      {
        EventAggregator.LockedFlag = enable;
      });
    }

    #endregion

    #region Get.

    /// <summary>
    /// Возвращает статус питания системы.
    /// </summary>
    /// <returns>true, если активно; false, если не активно.</returns>
    public static async Task<bool> GetIsActivePower() => await Task.Run(() => IsActivePower);

    /// <summary>
    /// Возвращает статус занятости программы.
    /// </summary>
    /// <returns>true, если активно; false, если не активно.</returns>
    public static async Task<bool> GetIsLocked() => await Task.Run(() => IsLocked);

    /// <summary>
    /// Возвращает текущий статус прав администратора.
    /// </summary>
    /// <returns>true, если запущено с правами администратора; false в противном случае.</returns>
    static public async Task<bool> GetAdminRights() => await Task.Run(() => IsAdmin);

    #endregion
  }
}
