using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Errors;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
{
  /// <summary>
  /// Содержит шаблоны ошибок, возникающих при парсинге выражений ПИ-команд.
  /// </summary>
  public static class PiErrors
  {
    /// <summary>
    /// Ошибка: выражение не распознано.
    /// </summary>
    public static ErrorItem CannotParseExpression(string expr, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pi_CannotParseExpression,
      Description = $"Не удалось распознать выражение: {expr}"
    };

    /// <summary>
    /// Ошибка: не удалось распознать параметры (напряжение, пороговое сопротивление, время).
    /// </summary>
    public static ErrorItem CannotParseParameters(string parameters, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pi_CannotParseParameters,
      Description = $"Не удалось распознать параметры: {parameters}"
    };

    /// <summary>
    /// Ошибка: не указаны точки для измерения.
    /// </summary>
    public static ErrorItem EmptyPoints(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pi_EmptyPoints,
      Description = "Не указаны точки для измерения."
    };

    /// <summary>
    /// Ошибка: команда ПИ не содержит ни одного параметра.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pi_EmptyCommandBody,
      Description = "Команда ПИ должна содержать хотя бы один параметр. Тело команды не может быть пустым."
    };

    /// <summary>
    /// Ошибка: команда ПИ не может содержать ключ Г, если для команды СИ присвоен ключ Т1.
    /// </summary>
    public static ErrorItem KeysConflict(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pi_KeysConflict,
      Description = "Команда ПИ не может содержать ключ Г, если для команды СИ присвоен ключ Т1."
    };
  }
}
