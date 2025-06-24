using System;
using Utilities.Errors;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
{
  /// <summary>
  /// Содержит шаблоны ошибок, возникающих при парсинге выражений СИ-команд.
  /// </summary>
  public static class SiErrors
  {
    /// <summary>
    /// Ошибка: выражение не распознано.
    /// </summary>
    public static ErrorItem CannotParseExpression(string expr, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Si_CannotParseExpression,
      Description = $"Не удалось распознать выражение: {expr}"
    };

    /// <summary>
    /// Ошибка: не удалось распознать параметры (напряжение, сопротивление, время).
    /// </summary>
    public static ErrorItem CannotParseParameters(string parameters, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Si_CannotParseParameters,
      Description = $"Не удалось распознать параметры: {parameters}"
    };

    /// <summary>
    /// Ошибка: не указаны точки для измерения.
    /// </summary>
    public static ErrorItem EmptyPoints(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Si_EmptyPoints,
      Description = "Не указаны точки для измерения."
    };

    /// <summary>
    /// Ошибка: команда СИ не содержит ни одного параметра.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Si_EmptyCommandBody,
      Description = "Команда СИ должна содержать хотя бы один параметр. Тело команды не может быть пустым."
    };
  }
}
