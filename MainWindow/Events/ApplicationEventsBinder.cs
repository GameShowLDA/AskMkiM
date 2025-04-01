namespace MainWindowProgram.Events
{
  /// <summary>
  /// Объединяет все подписчики событий приложения и выполняет их регистрацию.
  /// </summary>
  public class ApplicationEventsBinder
  {
    private readonly SystemEventsBinder _systemEvents;
    private readonly UiEventsBinder _uiEvents;
    private readonly StateEventsBinder _stateEvents;

    public ApplicationEventsBinder(SystemEventsBinder systemEvents, UiEventsBinder uiEvents, StateEventsBinder stateEvents)
    {
      _systemEvents = systemEvents;
      _uiEvents = uiEvents;
      _stateEvents = stateEvents;
    }

    /// <summary>
    /// Выполняет подписку на все события приложения.
    /// </summary>
    public void BindAll()
    {
      _systemEvents.Bind();
      _uiEvents.Bind();
      _stateEvents.Bind();
    }
  }
}
