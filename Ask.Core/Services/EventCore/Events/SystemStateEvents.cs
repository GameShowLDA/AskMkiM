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
      /// Указывает, доступны ли отладочные функции.
      /// </summary>
      public bool IsDebugEnabled { get; }

      /// <summary>
      /// Создаёт новое событие изменения доступности отладочных функций.
      /// </summary>
      /// <param name="isDebugEnabled">Признак доступности отладочных функций.</param>
      public DebugRightsChanged(bool isDebugEnabled)
      {
        IsDebugEnabled = isDebugEnabled;
      }
    }

    /// <summary>
    /// Событие, обозначающее изменение доступа к консоли администратора.
    /// </summary>
    public class ConsoleAccessChanged : IEvent
    {
      /// <summary>
      /// Указывает, доступна ли консоль администратора.
      /// </summary>
      public bool IsEnabled { get; }

      /// <summary>
      /// Создаёт новое событие изменения доступа к консоли администратора.
      /// </summary>
      /// <param name="isEnabled">Новое состояние доступа: true — консоль доступна; false — консоль скрыта и недоступна.</param>
      public ConsoleAccessChanged(bool isEnabled)
      {
        IsEnabled = isEnabled;
      }
    }

    /// <summary>
    /// Событие, обозначающее изменение видимости меню испытаний.
    /// </summary>
    public class TestsMenuVisibilityChanged : IEvent
    {
      /// <summary>
      /// Указывает, должно ли меню испытаний отображаться.
      /// </summary>
      public bool IsVisible { get; }

      /// <summary>
      /// Создаёт новое событие изменения видимости меню испытаний.
      /// </summary>
      /// <param name="isVisible">Новое состояние видимости: true — показать меню; false — скрыть меню.</param>
      public TestsMenuVisibilityChanged(bool isVisible)
      {
        IsVisible = isVisible;
      }
    }

    /// <summary>
    /// Событие, обозначающее изменение доступа к редактированию конфигурации.
    /// </summary>
    public class DeviceConfigurationEditingAccessChanged : IEvent
    {
      /// <summary>
      /// Указывает, доступно ли редактирование конфигурации.
      /// </summary>
      public bool IsEnabled { get; }

      /// <summary>
      /// Создаёт новое событие изменения доступа к редактированию конфигурации.
      /// </summary>
      /// <param name="isEnabled">Новое состояние доступа: true — редактирование доступно; false — редактирование скрыто и недоступно.</param>
      public DeviceConfigurationEditingAccessChanged(bool isEnabled)
      {
        IsEnabled = isEnabled;
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
