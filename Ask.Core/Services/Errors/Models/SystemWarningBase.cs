namespace Ask.Core.Services.Errors.Models
{
  /// <summary>
  /// Представляет системное предупреждение, содержащее описание,
  /// код и дополнительные данные для отображения и диагностики.
  /// </summary>
  public class SystemWarningBase
  {
    /// <summary>
    /// Объект ошибки, содержащий код и описание.
    /// </summary>
    public WarningItem Warning { get; }

    /// <summary>
    /// Код ошибки, если он указан.
    /// </summary>
    public WarningCode? Code => Warning?.Code;

    /// <summary>
    /// Описание ошибки.
    /// </summary>
    public string Description => Warning?.Description;

    /// <summary>
    /// Инициализирует новое системное исключение с объектом ошибки.
    /// </summary>
    /// <param name="warning">Объект ошибки, содержащий сведения о коде и описании.</param>
    public SystemWarningBase(WarningItem warning)
    {
      Warning = warning ?? new WarningItem
      {
        Description = "Ошибка не определена.",
        Code = WarningCode.Unknown
      };
    }

    /// <summary>
    /// Инициализирует новое системное исключение по коду ошибки и описанию.
    /// </summary>
    /// <param name="code">Код ошибки.</param>
    /// <param name="description">Описание ошибки.</param>
    public SystemWarningBase(WarningCode code, string description)
    {
      Warning = new WarningItem
      {
        Code = code,
        Description = description
      };
    }

    /// <summary>
    /// Инициализирует новое системное исключение с вложенным исключением.
    /// </summary>
    /// <param name="warning">Описание ошибки.</param>
    /// <param name="innerException">Вложенное исключение.</param>
    public SystemWarningBase(WarningItem warning, Exception innerException)
    {
      Warning = warning ?? new WarningItem
      {
        Description = "Ошибка не определена.",
        Code = WarningCode.Unknown
      };
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
