namespace Ask.Core.Services.Errors.Models
{
  /// <summary>
  /// Базовый тип исключения для всех системных компонентов проекта.
  /// Поддерживает структурированное описание ошибки через <see cref="ErrorItem"/>.
  /// </summary>
  public class SystemExceptionBase : Exception
  {
    /// <summary>
    /// Объект ошибки, содержащий код и описание.
    /// </summary>
    public ErrorItem Error { get; }

    /// <summary>
    /// Код ошибки, если он указан.
    /// </summary>
    public ErrorCode? Code => Error?.Code;

    /// <summary>
    /// Описание ошибки.
    /// </summary>
    public string Description => Error?.Description ?? "Неизвестная ошибка.";

    /// <summary>
    /// Инициализирует новое системное исключение с объектом ошибки.
    /// </summary>
    /// <param name="error">Объект ошибки, содержащий сведения о коде и описании.</param>
    public SystemExceptionBase(ErrorItem error)
      : base(error?.Description ?? "Неизвестная ошибка.")
    {
      Error = error ?? new ErrorItem
      {
        Description = "Ошибка не определена.",
        Code = ErrorCode.Unknown
      };
    }

    /// <summary>
    /// Инициализирует новое системное исключение по коду ошибки и описанию.
    /// </summary>
    /// <param name="code">Код ошибки.</param>
    /// <param name="description">Описание ошибки.</param>
    public SystemExceptionBase(ErrorCode code, string description)
      : base(description)
    {
      Error = new ErrorItem
      {
        Code = code,
        Description = description
      };
    }

    /// <summary>
    /// Инициализирует новое системное исключение с вложенным исключением.
    /// </summary>
    /// <param name="error">Объект ошибки.</param>
    /// <param name="inner">Вложенное исключение.</param>
    public SystemExceptionBase(ErrorItem error, Exception inner)
      : base(error?.Description ?? "Ошибка системы.", inner)
    {
      Error = error;
    }

    /// <summary>
    /// Возвращает текстовое представление исключения в формате:
    /// [КОД] Описание.
    /// </summary>
    public override string ToString()
    {
      string code = Code?.ToString() ?? "UNKNOWN";
      return $"[{code}] {Description}";
    }
  }
}
