namespace Ask.Core.Services.Errors.DataBase
{
  /// <summary>
  /// Базовый тип исключений, связанных с операциями базы данных.
  /// Используется как родитель для всех конкретных ошибок уровня БД.
  /// </summary>
  public class DatabaseException : Exception
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DatabaseException"/>.
    /// </summary>
    public DatabaseException() { }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DatabaseException"/> с сообщением об ошибке.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public DatabaseException(string message) : base(message) { }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DatabaseException"/> с сообщением и внутренним исключением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутреннее исключение, вызвавшее текущее.</param>
    public DatabaseException(string message, Exception innerException)
      : base(message, innerException) { }
  }
}
