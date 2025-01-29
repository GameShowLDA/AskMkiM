using static AppConfig.Config.SystemStateManager;

namespace AppConfig
{
  /// <summary>
  /// 
  /// </summary>
  public static class EventAggregator
  {
    /// <summary>
    /// Событие, которое вызывается при изменении состояния питания.
    /// </summary>
    static public event Action<bool> PowerChanged;

    /// <summary>
    /// Событие, которое вызывается при изменении состояния блокировки.
    /// </summary>
    static public event Action<bool> LockedChanged;

    /// <summary>
    /// Событие, которое вызывается при возникновении ошибки.
    /// </summary>
    static public event Action<string, bool> ErrorMessageEvent;

    /// <summary>
    /// Событие, которое вызывается при возникновении предупреждения.
    /// </summary>
    static public event Action<string, bool> WarningMessageEvent;

    /// <summary>
    /// Событие, которое вызывается при возникновении информационного сообщения.
    /// </summary>
    static public event Action<string, bool> InfoMessageEvent;

    /// <summary>
    /// Событие, которое вызывается при изменении состояния подключения USB устройства.
    /// </summary>
    static public event Action<bool> AdminRightsChanged;

    /// <summary>
    /// Событие, которое вызывается при изменении статуса прав администратора.
    /// </summary>
    static internal bool AdminRightsFlag
    {
      get => IsAdmin;
      set
      {
        if (IsAdmin != value)
        {
          IsAdmin = value;
          AdminRightsChanged?.Invoke(IsAdmin);
        }
      }
    }

    /// <summary>
    /// Флаг, указывающий, активно ли питание системы.
    /// </summary>
    static internal bool PowerFlag
    {
      get => IsActivePower;
      set
      {
        if (IsActivePower != value)
        {
          IsActivePower = value;
          PowerChanged?.Invoke(IsActivePower);
        }
      }
    }

    /// <summary>
    /// Флаг, указывающий, активно ли питание системы.
    /// </summary>
    static internal bool LockedFlag
    {
      get => IsLocked;
      set
      {
        if (IsLocked != value)
        {
          IsLocked = value;
          LockedChanged?.Invoke(IsLocked);
        }
      }
    }

    /// <summary>
    /// Метод вывода ошибки в блок информации программы.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="clearMessage">Удалить сообщение?</param>
    static public void RaiseErrorMessage(string message, bool clearMessage = false)
    {
      ErrorMessageEvent?.Invoke(message, clearMessage);
    }

    /// <summary>
    /// Метод вывода предупреждения в блок информации программы.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="clearMessage">Удалить сообщение?</param>
    static public void RaiseWarningMessage(string message, bool clearMessage = false)
    {
      WarningMessageEvent?.Invoke(message, clearMessage);
    }

    /// <summary>
    /// Метод вывода информации в блок информации программы.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="clearMessage">Удалить сообщение?</param>
    static public void RaiseInfoMessage(string message, bool clearMessage = false)
    {
      InfoMessageEvent?.Invoke(message, clearMessage);
    }
  }
}
