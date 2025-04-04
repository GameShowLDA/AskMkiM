namespace ConsoleUtilities.Core
{
  /// <summary>
  /// Интерфейс обработчика пользовательского ввода консольных команд.
  /// </summary>
  public interface ICommandHandler
  {
    /// <summary>
    /// Обрабатывает пользовательский ввод, выполняя соответствующую команду.
    /// </summary>
    /// <param name="input">Введённая пользователем строка.</param>
    Task HandleInputAsync(string input);
  }
}
