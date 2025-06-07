using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Base;

namespace AppConfiguration.SystemState
{
  /// <summary>
  /// Менеджер состояния системы, управляющий правами доступа, питанием и блокировкой интерфейса.
  /// </summary>
  static public class SystemStateManager
  {
    #region Properties.

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

    #endregion
  }
}
