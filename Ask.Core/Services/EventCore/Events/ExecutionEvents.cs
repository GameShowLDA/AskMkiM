using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.EventInterfaces;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;

namespace Ask.Core.Services.EventCore.Events
{
  /// <summary>
  /// Содержит события, связанные с управлением выполнением
  /// и состоянием процесса исполнения.
  /// </summary>
  /// <remarks>
  /// Используется для передачи событий выполнения, таких как
  /// изменение пошагового режима, нажатия управляющих кнопок
  /// и взаимодействие с breakpoint.
  /// </remarks>
  public static class ExecutionEvents
  {
    /// <summary>
    /// Событие изменения состояния пошагового режима выполнения.
    /// </summary>
    /// <remarks>
    /// Используется для уведомления подписчиков о включении
    /// или отключении пошагового режима.
    /// </remarks>
    public class StepByStepModeChanged : IEvent
    {
      /// <summary>
      /// Признак того, включён ли пошаговый режим.
      /// </summary>
      public bool IsEnabled { get; }

      /// <summary>
      /// Создаёт экземпляр события изменения состояния пошагового режима.
      /// </summary>
      /// <param name="isEnabled">
      /// <see langword="true"/> — если пошаговый режим включён;
      /// <see langword="false"/> — если режим отключён.
      /// </param>
      public StepByStepModeChanged(bool isEnabled)
      {
        IsEnabled = isEnabled;
      }
    }

    /// <summary>
    /// Событие изменения списка активных устройств.
    /// </summary>
    public class ActiveDeviceChanged : IEvent
    {
      /// <summary>
      /// Список устройств, ставших активными.
      /// </summary>
      public List<IAttachableDevice> Devices { get; }

      /// <summary>
      /// Создаёт событие изменения активных устройств.
      /// </summary>
      /// <param name="devices">
      /// Список активных устройств.
      /// </param>
      public ActiveDeviceChanged(List<IAttachableDevice> devices)
      {
        Devices = devices;
      }
    }

    /// <summary>
    /// Событие обновления состояния устройств.
    /// </summary>
    public class DeviceStatusUpdate : IEvent { }

    /// <summary>
    /// Событие нажатия управляющей кнопки выполнения.
    /// </summary>
    public class ControlButtonPressed : IEvent
    {
      /// <summary>
      /// Кнопка управления, вызвавшая событие.
      /// </summary>
      public ExecutionControlButton Button { get; }

      /// <summary>
      /// Создаёт событие нажатия управляющей кнопки.
      /// </summary>
      /// <param name="button">
      /// Нажатая кнопка управления выполнением.
      /// </param>
      public ControlButtonPressed(ExecutionControlButton button)
      {
        Button = button;
      }
    }

    /// <summary>
    /// Событие нажатия F4 на команде,
    /// поддерживающей breakpoint.
    /// </summary>
    public class BreakpointF4Pressed : IEvent
    {
      /// <summary>
      /// Команда, на которой был вызван breakpoint.
      /// </summary>
      public IExecutionCommandInfo CommandInfo { get; }

      /// <summary>
      /// Создаёт событие срабатывания breakpoint через F4.
      /// </summary>
      /// <param name="commandInfo">
      /// Информация о команде, на которой был установлен breakpoint.
      /// </param>
      public BreakpointF4Pressed(IExecutionCommandInfo commandInfo)
      {
        CommandInfo = commandInfo;
      }
    }
  }
}
