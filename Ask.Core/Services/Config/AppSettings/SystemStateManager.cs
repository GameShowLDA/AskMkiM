using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using System.Diagnostics;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Менеджер состояния системы, управляющий питанием и блокировкой интерфейса.
  /// </summary>
  public static class SystemStateManager
  {
    #region Properties.

    /// <summary>
    /// Таймер для измерения времени выполнения операций.
    /// </summary>
    public static readonly Stopwatch _stopwatch = new();

    /// <summary>
    /// Флаг, указывающий, активно ли питание системы.
    /// </summary>
    internal static bool IsActivePower { get; private set; }

    /// <summary>
    /// Флаг, указывающий, находится ли система в состоянии блокировки.
    /// </summary>
    internal static bool IsLocked { get; private set; }

    internal static bool IsControlProgramActive { get; private set; }

    #endregion

    #region Constructor.

    /// <summary>
    /// Статический конструктор.
    /// Подписывается на события <see cref="SystemStateEvents.PowerChanged"/> и <see cref="SystemStateEvents.LockedChanged"/>,
    /// обеспечивая автоматическую синхронизацию внутренних флагов состояния.
    /// </summary>
    static SystemStateManager()
    {
      EventAggregator.Subscribe<SystemStateEvents.PowerChanged>(e => IsActivePower = e.IsPowered);
      EventAggregator.Subscribe<SystemStateEvents.LockedChanged>(e => IsLocked = e.IsLocked);
    }

    #endregion

    #region Set.

    /// <summary>
    /// Включает или выключает питание системы и публикует соответствующее событие.
    /// </summary>
    /// <param name="enable">true — включить питание; false — выключить.</param>
    public static async Task SetIsActivePower(bool enable)
    {
      await Task.Run(() =>
      {
        SystemStateEventAdapter.RaisePowerChanged(enable);
      });
    }

    /// <summary>
    /// Включает или отключает блокировку интерфейса и публикует соответствующее событие.
    /// </summary>
    /// <param name="enable">true — заблокировать; false — разблокировать.</param>
    public static async Task SetIsLocked(bool enable)
    {
      await Task.Run(() =>
      {
        IsLocked = enable;
        SystemStateEventAdapter.RaiseLockedChanged(enable);
      });
    }

    /// <summary>
    /// Включает или отключает блокировку интерфейса и публикует соответствующее событие.
    /// </summary>
    /// <param name="enable">true — заблокировать; false — разблокировать.</param>
    public static async Task SetIsControlProgramActive(bool enable)
    {
      await Task.Run(() =>
      {
        IsControlProgramActive = enable;
        SystemStateEventAdapter.RaiseControlProgramActiveChanged(enable);
      });
    }

    #endregion

    #region Get.

    /// <summary>
    /// Асинхронно возвращает текущий статус питания системы.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если питание активно; <see langword="false"/> — если отключено.
    /// </returns>
    public static async Task<bool> GetIsActivePower() =>
      await Task.Run(() => IsActivePower);

    /// <summary>
    /// Асинхронно возвращает текущий статус блокировки интерфейса.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если интерфейс заблокирован; <see langword="false"/> — если разблокирован.
    /// </returns>
    public static bool GetIsLocked() =>IsLocked;

    /// <summary>
    /// Асинхронно возвращает текущий статус блокировки интерфейса.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если интерфейс заблокирован; <see langword="false"/> — если разблокирован.
    /// </returns>
    public static async Task<bool> GetIsControlProgramActive() =>
      await Task.Run(() => IsControlProgramActive);

    #endregion
  }
}
