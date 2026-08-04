namespace Ask.Core.Services.Errors.Models
{
  /// <summary>
  /// Представляет ожидаемую ошибку пользовательского ввода, не являющуюся сбоем приложения.
  /// </summary>
  public sealed class InputValidationException : SystemExceptionBase
  {
    /// <summary>
    /// Создаёт исключение проверки пользовательского ввода.
    /// </summary>
    /// <param name="error">Сведения об ошибке пользовательского ввода.</param>
    public InputValidationException(ErrorItem error)
      : base(error)
    {
    }
  }
}
