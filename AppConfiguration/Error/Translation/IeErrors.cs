using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Errors;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
{
  public class IeErrors
  {
    /// <summary>
    /// Ошибка: команда ИЕ не содержит ни одной границы емкости.
    /// </summary>
    public static ErrorItem EmptyCapacity(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Ie_EmptyCapacity,
      Description = "Команда ИЕ должна содержать хотя бы одну из границ емкости. Емкость не может быть не задано."
    };

    /// <summary>
    /// Ошибка: не удалось распознать параметры.
    /// </summary>
    public static ErrorItem CannotParseParameters(string parameters, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Ie_CannotParseParameters,
      Description = $"Не удалось распознать параметры: {parameters}"
    };

    /// <summary>
    /// Ошибка: не указаны точки для измерения.
    /// </summary>
    public static ErrorItem EmptyPoints(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Ie_EmptyPoints,
      Description = "Не указаны точки для измерения."
    };


    /// <summary>
    /// Ошибка: команда ИЕ не содержит ни одного параметра.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Ie_EmptyCommandBody,
      Description = "Команда ИЕ должна содержать хотя бы один параметр. Тело команды не может быть пустым."
    };
  }
}
