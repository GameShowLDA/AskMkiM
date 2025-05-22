namespace MainWindowProgram.Events
{
  /// <summary>
  /// Объединяет все подписчики событий приложения и выполняет их регистрацию.
  /// </summary>
  public class ApplicationEventsBinder
  {
    internal readonly SystemEventsBinder SystemEvents;
    internal readonly UiEventsBinder UiEvents;
    internal readonly StateEventsBinder StateEvents;

    public ApplicationEventsBinder(SystemEventsBinder systemEvents, UiEventsBinder uiEvents, StateEventsBinder stateEvents)
    {
      SystemEvents = systemEvents;
      UiEvents = uiEvents;
      StateEvents = stateEvents;
    }

    /// <summary>
    /// Выполняет подписку на все события приложения.
    /// </summary>
    public void BindAll()
    {
      SystemEvents.Bind();
      UiEvents.Bind();
      StateEvents.Bind();
    }
  }
}
