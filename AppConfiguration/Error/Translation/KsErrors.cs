using Utilities.Errors;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
{
  /// <summary>
  /// Содержит шаблоны ошибок, возникающих при парсинге выражений КС-команд.
  /// </summary>
  public static class KsErrors
  {
    /// <summary>
    /// Ошибка: команда КС не содержит ни одной границы для сопротивления.
    /// </summary>
    public static ErrorItem EmptyResistance(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Ks_EmptyResistance,
      Description = "Команда КС должна содержать хотя бы одну из границ сопротивления. Сопротивление не может быть не задано."
    };

    /// <summary>
    /// Ошибка: не удалось распознать параметры (напряжение, сопротивление, время).
    /// </summary>
    public static ErrorItem CannotParseParameters(string parameters, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Ks_CannotParseParameters,
      Description = $"Не удалось распознать параметры: {parameters}"
    };

    /// <summary>
    /// Ошибка: не указаны точки для измерения.
    /// </summary>
    public static ErrorItem EmptyPoints(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Ks_EmptyPoints,
      Description = "Не указаны точки для измерения."
    };


    /// <summary>
    /// Ошибка: команда КС не содержит ни одного параметра.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Ks_EmptyCommandBody,
      Description = "Команда КС должна содержать хотя бы один параметр. Тело команды не может быть пустым."
    };
  }
}
