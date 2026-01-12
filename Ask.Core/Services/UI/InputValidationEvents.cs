namespace Ask.Core.Services.UI
{
  /// <summary>
  /// Глобальные события, связанные с валидацией ввода данных.
  /// </summary>
  public static class InputValidationEvents
  {
    /// <summary>
    /// Событие при ошибке формата первой точки.
    /// </summary>
    public static event Action OnInvalidFirstPoint;

    /// <summary>
    /// Событие при ошибке формата второй точки.
    /// </summary>
    public static event Action OnInvalidSecondPoint;

    /// <summary>
    /// Событие при совпадении двух точек.
    /// </summary>
    public static event Action OnDuplicatePoints;

    /// <summary>
    /// Событие при ошибке электрического параметра.
    /// </summary>
    public static event Action OnInvalidElectricalParameter;

    private static bool _triggerInvalidFirstPoint;

    /// <summary>
    /// Флаг для вызова события <see cref="OnInvalidFirstPoint"/>.
    /// </summary>
    public static bool TriggerInvalidFirstPoint
    {
      get => _triggerInvalidFirstPoint;
      set
      {
        if (value)
        {
          _triggerInvalidFirstPoint = false;
          InvokeOnUIThread(OnInvalidFirstPoint);
        }
      }
    }

    private static bool _triggerInvalidSecondPoint;

    /// <summary>
    /// Флаг для вызова события <see cref="OnInvalidSecondPoint"/>.
    /// </summary>
    public static bool TriggerInvalidSecondPoint
    {
      get => _triggerInvalidSecondPoint;
      set
      {
        if (value)
        {
          _triggerInvalidSecondPoint = false;
          InvokeOnUIThread(OnInvalidSecondPoint);
        }
      }
    }

    private static bool _triggerDuplicatePoints;

    /// <summary>
    /// Флаг для вызова события <see cref="OnDuplicatePoints"/>.
    /// </summary>
    public static bool TriggerDuplicatePoints
    {
      get => _triggerDuplicatePoints;
      set
      {
        if (value)
        {
          _triggerDuplicatePoints = false;
          InvokeOnUIThread(OnDuplicatePoints);
        }
      }
    }

    private static bool _triggerInvalidParameter;

    /// <summary>
    /// Флаг для вызова события <see cref="OnInvalidElectricalParameter"/>.
    /// </summary>
    public static bool TriggerInvalidParameter
    {
      get => _triggerInvalidParameter;
      set
      {
        if (value)
        {
          _triggerInvalidParameter = false;
          InvokeOnUIThread(OnInvalidElectricalParameter);
        }
      }
    }

    /// <summary>
    /// Выполняет вызов события в UI-потоке.
    /// </summary>
    /// <param name="action">Действие события.</param>
    private static void InvokeOnUIThread(Action action)
    {
      if (action == null)
      {
        return;
      }

      var dispatcher = System.Windows.Application.Current?.Dispatcher;

      if (dispatcher != null && !dispatcher.CheckAccess())
      {
        dispatcher.Invoke(action);
      }
      else
      {
        action.Invoke();
      }
    }
  }
}
