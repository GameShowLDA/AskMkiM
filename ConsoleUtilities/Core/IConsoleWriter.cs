namespace ConsoleUtilities.Core
{
  /// <summary>
  /// Интерфейс для абстракции вывода в консоль.
  /// </summary>
  public interface IConsoleWriter
  {
    /// <summary>
    /// Печатает строку без перехода на новую.
    /// </summary>
    /// <param name="message">Сообщение для вывода.</param>
    void Write(string message);

    /// <summary>
    /// Печатает строку с переходом на новую.
    /// </summary>
    /// <param name="message">Сообщение для вывода.</param>
    void WriteLine(string message);

    /// <summary>
    /// Очищает текущий вывод в консоли.
    /// </summary>
    void Clear();
  }
}
