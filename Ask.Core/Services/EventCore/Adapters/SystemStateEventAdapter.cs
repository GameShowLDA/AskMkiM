using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;

namespace Ask.Core.Services.EventCore.Adapters
{
  /// <summary>
  /// Предоставляет адаптационный слой между старой моделью управления состоянием системы
  /// и новой архитектурой событий <see cref="EventCore"/>.
  /// </summary>
  /// <remarks>
  /// Адаптер используется для генерации событий <see cref="SystemStateEvents"/>,
  /// обеспечивая обратную совместимость со старой системой через привычные методы
  /// (<see cref="RaisePowerChanged"/>, <see cref="RaiseLockedChanged"/>, <see cref="RaiseAdminRightsChanged"/>).
  /// </remarks>
  public static class SystemStateEventAdapter
  {
    /// <summary>
    /// Генерирует событие изменения состояния питания.
    /// </summary>
    /// <param name="isPowered">Новое состояние питания: true — питание включено; false — отключено.</param>
    /// <example>
    /// <code>
    /// SystemStateEventAdapter.RaisePowerChanged(true);
    /// </code>
    /// </example>
    public static void RaisePowerChanged(bool isPowered) =>
      EventAggregator.Publish(new SystemStateEvents.PowerChanged(isPowered));

    /// <summary>
    /// Генерирует событие изменения состояния блокировки интерфейса.
    /// </summary>
    /// <param name="isLocked">Новое состояние блокировки: true — интерфейс заблокирован; false — разблокирован.</param>
    /// <example>
    /// <code>
    /// SystemStateEventAdapter.RaiseLockedChanged(false);
    /// </code>
    /// </example>
    public static void RaiseLockedChanged(bool isLocked) =>
      EventAggregator.Publish(new SystemStateEvents.LockedChanged(isLocked));

    /// <summary>
    /// Генерирует событие изменения состояния активности программы контроля.
    /// </summary>
    /// <param name="isLocked">Новое состояние ПК: true — ПК активна на экране; false — ПК не активна на экране.</param>
    /// <example>
    /// <code>
    /// SystemStateEventAdapter.RaiseLockedChanged(false);
    /// </code>
    /// </example>
    public static void RaiseControlProgramActiveChanged(bool isControlProgramActive) =>
      EventAggregator.Publish(new SystemStateEvents.ControlProgramActiveChanged(isControlProgramActive));

    /// <summary>
    /// Генерирует событие изменения прав администратора.
    /// </summary>
    /// <param name="isAdmin">Новое состояние прав администратора: true — активен режим администратора; false — обычный пользователь.</param>
    /// <example>
    /// <code>
    /// SystemStateEventAdapter.RaiseAdminRightsChanged(true);
    /// </code>
    /// </example>
    public static void RaiseAdminRightsChanged(bool isAdmin) =>
      EventAggregator.Publish(new SystemStateEvents.AdminRightsChanged(isAdmin));

    /// <summary>
    /// Генерирует событие изменения прав отладки (для программисотов).
    /// </summary>
    /// <param name="isDebug">Новое состояние прав отладки: true — активен режим отладки; false — обычный пользователь.</param>
    /// <example>
    /// <code>
    /// SystemStateEventAdapter.RaiseAdminRightsChanged(true);
    /// </code>
    /// </example>
    public static void RaiseDebugRightsChanged(bool isDebug) =>
      EventAggregator.Publish(new SystemStateEvents.DebugRightsChanged(isDebug));

    /// <summary>
    /// Генерирует событие изменения доступа к консоли администратора.
    /// </summary>
    /// <param name="isEnabled">Новое состояние доступа к консоли: true — доступна; false — скрыта и недоступна.</param>
    public static void RaiseConsoleAccessChanged(bool isEnabled) =>
      EventAggregator.Publish(new SystemStateEvents.ConsoleAccessChanged(isEnabled));

    /// <summary>
    /// Генерирует событие изменения видимости меню испытаний.
    /// </summary>
    /// <param name="isVisible">Новое состояние видимости меню: true — показать; false — скрыть.</param>
    public static void RaiseTestsMenuVisibilityChanged(bool isVisible) =>
      EventAggregator.Publish(new SystemStateEvents.TestsMenuVisibilityChanged(isVisible));
  }
}
