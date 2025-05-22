using System.Diagnostics;

namespace ConsoleUtilities.Services
{
  /// <summary>
  /// Проверяет существование команд в системной среде Windows CMD.
  /// </summary>
  public static class CmdCommandValidator
  {
    private static readonly HashSet<string> BuiltInCmdCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "assoc", "break", "call", "cd", "chdir", "cls", "color", "copy", "date", "del", "dir", "echo",
            "endlocal", "erase", "exit", "for", "ftype", "goto", "if", "md", "mkdir", "move", "path",
            "pause", "popd", "prompt", "pushd", "rd", "rem", "ren", "rename", "rmdir", "set", "setlocal",
            "shift", "start", "time", "title", "type", "ver", "verify", "vol"
        };

    /// <summary>
    /// Проверяет, доступна ли команда как встроенная или внешняя команда Windows.
    /// </summary>
    /// <param name="command">Имя команды для проверки.</param>
    /// <returns><c>true</c>, если команда существует и может быть выполнена через CMD; иначе — <c>false</c>.</returns>
    public static bool IsCmdCommand(string command)
    {
      if (string.IsNullOrWhiteSpace(command))
        return false;

      if (BuiltInCmdCommands.Contains(command))
        return true;

      return IsExternalExecutable(command);
    }

    /// <summary>
    /// Использует системную утилиту "where" для поиска команды во внешних путях.
    /// </summary>
    /// <param name="command">Имя исполняемого файла.</param>
    /// <returns><c>true</c>, если команда найдена в PATH; иначе — <c>false</c>.</returns>
    private static bool IsExternalExecutable(string command)
    {
      try
      {
        var psi = new ProcessStartInfo
        {
          FileName = "where",
          Arguments = command,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
          return false;

        process.WaitForExit(1000);
        return process.ExitCode == 0;
      }
      catch
      {
        return false;
      }
    }
  }
}
