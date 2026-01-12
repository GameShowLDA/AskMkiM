namespace Ask.Core.Shared.Interfaces.UiInterfaces
{
  /// <summary>
  /// Представляет абстракцию источника поля ввода.
  /// Позволяет получить объект доступа к данным, не раскрывая UI-реализацию.
  /// </summary>
  public interface IInputFieldProvider
  {
    /// <summary>
    /// Возвращает абстракцию поля ввода,
    /// либо <c>null</c>, если элемент отсутствует.
    /// </summary>
    IInputFieldAccessor? GetInputFieldAccessor();

    IInputHighlightService GetInputHighlightService();
  }
}
