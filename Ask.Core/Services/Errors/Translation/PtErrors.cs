using Ask.Core.Services.Errors.Models;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ask.Core.Services.Errors.Translation
{
  public class PtErrors
  {
    /// <summary>
    /// Ошибка: команда ПР не содержит ни одного параметра.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Pt_EmptyCommandBody,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Команда ПТ должна содержать хотя бы один параметр. Тело команды не может быть пустым."
      };

    /// <summary>
    /// Ошибка: не указаны точки для измерения.
    /// </summary>
    public static ErrorItem EmptyPoints(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Pt_EmptyPoints,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Не указаны точки для подключения."
      };
  }
}
