using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.EventInterfaces;

namespace Ask.Core.Services.EventCore.Adapters
{
  /// <summary>
  /// Адаптер для генерации событий <see cref="ExecutionEvents"/>. 
  /// Предоставляет удобный способ уведомлять систему об изменении состояния выполнения алгоритмов.
  /// </summary>
  /// <remarks>
  /// Используется для обратной совместимости с предыдущей системой событий <c>EventAggregator</c>,
  /// а также для публикации новых событий выполнения, таких как включение или выключение пошагового режима.
  /// </remarks>
  public static class ExecutionEventAdapter
  {
    /// <summary>
    /// Генерирует событие изменения состояния пошагового режима выполнения алгоритма.
    /// </summary>
    /// <param name="isEnabled">
    /// <see langword="true"/> — если пошаговый режим включён;  
    /// <see langword="false"/> — если он выключен.
    /// </param>
    /// <example>
    /// <code>
    /// ExecutionEventAdapter.RaiseStepByStepModeChanged(true);
    /// </code>
    /// </example>
    public static void RaiseStepByStepModeChanged(bool isEnabled)
      => EventAggregator.Publish(new ExecutionEvents.StepByStepModeChanged(isEnabled));

    public static void RaiseDevicesChanged(List<IDevice> devices)
      => EventAggregator.Publish(new ExecutionEvents.ActiveDeviceChanged(devices));
  }
}
