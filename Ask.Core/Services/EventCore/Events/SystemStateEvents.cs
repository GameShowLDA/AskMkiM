using Ask.Core.Shared.Interfaces.EventInterfaces;

namespace Ask.Core.Services.EventCore.Events
{
  /// <summary>
  /// Содержит события, отражающие текущее состояние системы —
  /// питание, блокировку и административные права пользователя.
  /// </summary>
  public static class SystemStateEvents
  {
    /// <summary>
    /// Событие, обозначающее изменение состояния питания системы.
    /// </summary>
    public class PowerChanged : IEvent
    {
      /// <summary>
      /// Указывает, активно ли питание системы.
      /// </summary>
      public bool IsPowered { get; }

      /// <summary>
      /// Создаёт новое событие изменения состояния питания.
      /// </summary>
      /// <param name="isPowered">Новое состояние питания: true — питание включено; false — отключено.</param>
      public PowerChanged(bool isPowered)
      {
        IsPowered = isPowered;
      }
    }

    /// <summary>
    /// Событие, обозначающее изменение состояния блокировки интерфейса.
    /// </summary>
    public class LockedChanged : IEvent
    {
      /// <summary>
      /// Указывает, заблокирована ли система.
      /// </summary>
      public bool IsLocked { get; }

      /// <summary>
      /// Создаёт новое событие изменения состояния блокировки.
      /// </summary>
      /// <param name="isLocked">Новое состояние блокировки: true — интерфейс заблокирован; false — разблокирован.</param>
      public LockedChanged(bool isLocked)
      {
        IsLocked = isLocked;
      }
    }

    /// <summary>
    /// Событие, обозначающее изменение состояния прав администратора.
    /// </summary>
    public class AdminRightsChanged : IEvent
    {
      /// <summary>
      /// Указывает, активен ли режим администратора.
      /// </summary>
      public bool IsAdmin { get; }

      /// <summary>
      /// Создаёт новое событие изменения прав администратора.
      /// </summary>
      /// <param name="isAdmin">Новое состояние прав администратора: true — права администратора активны; false — обычный пользователь.</param>
      public AdminRightsChanged(bool isAdmin)
      {
        IsAdmin = isAdmin;
      }
    }

    /// <summary>
    /// Событие, обозначающее изменение состояния прав администратора.
    /// </summary>
    public class DebugRightsChanged : IEvent
    {
      /// <summary>
      /// Указывает, активен ли режим администратора.
      /// </summary>
      public bool IsDebug { get; }

      /// <summary>
      /// Создаёт новое событие изменения прав администратора.
      /// </summary>
      /// <param name="isAdmin">Новое состояние прав администратора: true — права администратора активны; false — обычный пользователь.</param>
      public DebugRightsChanged(bool isAdmin)
      {
        IsDebug = isAdmin;
      }
    }

    /// <summary>
    /// Событие, обозначающее изменение состояния прав администратора.
    /// </summary>
    public class ControlProgramActiveChanged : IEvent
    {
      /// <summary>
      /// Указывает, активен ли документ, который можно выполнить.
      /// </summary>
      public bool IsControlProgramActive { get; }

      /// <summary>
      /// Создаёт новое событие изменения изменения видимости кнопки "Выполнить".
      /// </summary>
      /// <param name="isControlProgramActive">Новое кнокпи "Выполнить": true — кнокпка активна; false — кнопка скрыта.</param>
      public ControlProgramActiveChanged(bool isControlProgramActive)
      {
        IsControlProgramActive = isControlProgramActive;
      }
    }
  }
}
