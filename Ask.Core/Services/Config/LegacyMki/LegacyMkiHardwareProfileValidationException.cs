namespace Ask.Core.Services.Config.LegacyMki;

/// <summary>
/// Исключение, которое выбрасывается при недопустимой legacy-конфигурации аппаратуры АСК.
/// </summary>
public sealed class LegacyMkiHardwareProfileValidationException : InvalidOperationException
{
  /// <summary>
  /// Создает исключение с набором найденных ошибок.
  /// </summary>
  /// <param name="errors">Список ошибок конфигурации.</param>
  public LegacyMkiHardwareProfileValidationException(IReadOnlyList<LegacyMkiHardwareProfileValidationError> errors)
    : base(BuildMessage(errors))
  {
    Errors = errors;
  }

  /// <summary>
  /// Возвращает найденные ошибки конфигурации.
  /// </summary>
  public IReadOnlyList<LegacyMkiHardwareProfileValidationError> Errors { get; }

  /// <summary>
  /// Собирает многострочное сообщение для окна ошибки и протокола.
  /// </summary>
  /// <param name="errors">Список ошибок конфигурации.</param>
  private static string BuildMessage(IReadOnlyList<LegacyMkiHardwareProfileValidationError> errors)
  {
    if (errors.Count == 0)
    {
      return "Конфигурация аппаратуры АСК содержит ошибку.";
    }

    return "Конфигурация аппаратуры АСК содержит ошибки:" + Environment.NewLine
      + string.Join(Environment.NewLine, errors.Select(error => "- " + error.Message));
  }
}
