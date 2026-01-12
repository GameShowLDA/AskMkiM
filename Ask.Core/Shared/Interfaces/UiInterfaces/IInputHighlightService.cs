namespace Ask.Core.Shared.Interfaces.UiInterfaces
{
  /// <summary>
  /// Интерфейс для подсветки полей ввода при ошибках.
  /// </summary>
  public interface IInputHighlightService
  {
    /// <summary>
    /// Подсвечивает поле ввода проверяемого номера при ошибке.
    /// </summary>
    void HighlightTestedNumber();

    /// <summary>
    /// Подсвечивает поле ввода проверяющего номера при ошибке.
    /// </summary>
    void HighlightTesterNumber();

    /// <summary>
    /// Подсвечивает поле ввода диапазона проверки при ошибке.
    /// </summary>
    void HighlightTestRange();
  }
}
