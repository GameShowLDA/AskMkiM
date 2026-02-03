using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;

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

    public static void RaiseDevicesChanged(List<IAttachableDevice> devices)
      => EventAggregator.Publish(new ExecutionEvents.ActiveDeviceChanged(devices));

    public static void RaiseDeviceStatusUpdate()
      => EventAggregator.Publish(new ExecutionEvents.DeviceStatusUpdate());

    /// <summary>
    /// Адаптер для публикации событий управления выполнением.
    /// </summary>
    public static class ExecutionControlEventAdapter
    {
      /// <summary>
      /// Публикует событие нажатия кнопки управления выполнением.
      /// </summary>
      /// <param name="button">Нажатая кнопка.</param>
      public static void Raise(ExecutionControlButton button) =>
        EventAggregator.Publish(new ExecutionEvents.ControlButtonPressed(button));
    }
  }
}
