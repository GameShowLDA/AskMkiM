using Ask.Core.Services.Errors.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Services.Errors.Translation
{
  public class VshErrors
  {
    /// <summary>
    /// Ошибка: в команде ВШ указана неверная структура стойки коммутации.
    /// </summary>
    public static ErrorItem InvalidVshBusStructure(int startLineNumber, string command, string numberStructure,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Vsh_InvalidVshBusStructure,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"В команде ВШ указана неверная структура стойки коммутации. Допускается только {numberStructure}."
      };

    /// <summary>
    /// Ошибка: в команде ВШ не указана структура стойки коммутации в конфигурации.
    /// </summary>
    public static ErrorItem NoneVshBusStructure(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Vsh_NoneVshBusStructure,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Системная ошибка: в конфигурации не указана структора стройки коммутации. Проверьте конфигурацию."
      };
  }
}
