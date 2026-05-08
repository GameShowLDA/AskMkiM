namespace ConsoleUI.ConsoleCommanding.Core
{
  public interface IConsoleWriter
  {
    /// <summary>
    /// Вывод строки с переводом строки.
    /// </summary>
    void WriteLine(string message);

    /// <summary>
    /// Очищает консоль.
    /// </summary>
    void Clear();

    Task<string> ReadLineAsync();
  }
}
