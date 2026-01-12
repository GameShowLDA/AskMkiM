namespace Ask.Core.Services.Errors.DataBase
{
  /// <summary>
  /// Исключение, возникающее при попытке добавить в базу данных дубликат записи.
  /// </summary>
  public class DuplicateEntityException : DatabaseException
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DuplicateEntityException"/>.
    /// </summary>
    public DuplicateEntityException() { }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DuplicateEntityException"/> с сообщением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public DuplicateEntityException(string message) : base(message) { }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DuplicateEntityException"/> с сообщением и внутренним исключением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public DuplicateEntityException(string message, Exception innerException)
      : base(message, innerException) { }
  }
}
