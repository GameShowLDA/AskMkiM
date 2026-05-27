using Ask.Core.Services.Errors.Models;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ask.Core.Services.Errors.Translation
{
  public class CkErrors
  {
    /// <summary>
    /// Ошибка: команда СК запрещена при двухшинной структуре коммутации.
    /// </summary>
    public static ErrorItem ForbiddenForTwoBusStructure(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Ck_ForbiddenForTwoBusStructure,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Команда СК запрещена при структуре стойки коммутации ВШ 2Ш. Используйте СК только в режиме ВШ 4Ш."
      };
  }
}
