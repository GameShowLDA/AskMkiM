using Ask.Core.Shared.Interfaces.EventInterfaces;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;

namespace Ask.Core.Services.EventCore.Events
{
  /// <summary>
  /// События, связанные с выполнением алгоритмов и управлением режимами выполнения.
  /// </summary>
  /// <remarks>
  /// Эти события используются для отслеживания изменений состояния выполнения, таких как
  /// включение пошагового режима, переход в автоматический режим и другие связанные состояния.
  /// </remarks>
  public static class ExecutionEvents
  {
    /// <summary>
    /// Событие, генерируемое при изменении состояния пошагового режима выполнения алгоритма.
    /// </summary>
    /// <remarks>
    /// Событие уведомляет все заинтересованные компоненты о том, что режим пошагового выполнения был включён или выключен.
    /// </remarks>
    public class StepByStepModeChanged : IEvent
    {
      /// <summary>
      /// Показывает, активирован ли пошаговый режим.
      /// </summary>
      public bool IsEnabled { get; }

      /// <summary>
      /// Инициализирует новый экземпляр события изменения состояния пошагового режима.
      /// </summary>
      /// <param name="isEnabled">
      /// <see langword="true"/> — если пошаговый режим включён;  
      /// <see langword="false"/> — если режим выключен.
      /// </param>
      public StepByStepModeChanged(bool isEnabled)
      {
        IsEnabled = isEnabled;
      }
    }

    /// <summary>
    /// Событие нажатия кнопки управления выполнением.
    /// </summary>
    public class ControlButtonPressed : IEvent
    {
      /// <summary>
      /// Какая кнопка была нажата.
      /// </summary>
      public ExecutionControlButton Button { get; }

      public ControlButtonPressed(ExecutionControlButton button)
      {
        Button = button;
      }
    }
  }
}
