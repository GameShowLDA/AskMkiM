using NLog;
using System.Runtime.CompilerServices;

namespace Ask.LogLib
{
  static public class LoggerUtility
  {
    /// <summary>
    /// Логирует информационное сообщение.
    /// </summary>
    /// <param name="message">Сообщение для логирования.</param>
    /// <param name="isDeviceLog">Если true, логируется в файл для оборудования.</param>
    /// <param name="callerFilePath">Путь к исходному файлу, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="lineNumber">Номер строки, откуда вызван метод. Заполняется автоматически.</param>
    /// <returns>Исходное сообщение.</returns>
    public static string LogInformation(string message, bool isDeviceLog = false, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int lineNumber = 0)
    {
      var logger = LogManager.GetLogger(GetLoggerName(callerFilePath, isDeviceLog));
      logger.Info(BuildMessage(message, callerFilePath, lineNumber));
      return message;
    }

    /// <summary>
    /// Логирует предупреждение.
    /// </summary>
    /// <param name="message">Сообщение для логирования.</param>
    /// <param name="isDeviceLog">Если true, логируется в файл для оборудования.</param>
    /// <param name="callerFilePath">Путь к исходному файлу, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="lineNumber">Номер строки, откуда вызван метод. Заполняется автоматически.</param>
    /// <returns>Исходное сообщение.</returns>
    public static string LogWarning(string message, bool isDeviceLog = false, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int lineNumber = 0)
    {
      var logger = LogManager.GetLogger(GetLoggerName(callerFilePath, isDeviceLog));
      logger.Warn(BuildMessage(message, callerFilePath, lineNumber));
      return message;
    }

    /// <summary>
    /// Логирует сообщение об ошибке.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="isDeviceLog">Если true, логируется в файл для оборудования.</param>
    /// <param name="callerFilePath">Путь к исходному файлу, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="lineNumber">Номер строки, откуда вызван метод. Заполняется автоматически.</param>
    /// <returns>Исходное сообщение.</returns>
    public static string LogError(string message, bool isDeviceLog = false, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int lineNumber = 0)
    {
      var logger = LogManager.GetLogger(GetLoggerName(callerFilePath, isDeviceLog));
      logger.Error(BuildMessage(message, callerFilePath, lineNumber));
      return message;
    }

    /// <summary>
    /// Логирует отладочное сообщение.
    /// </summary>
    /// <param name="message">Сообщение для логирования.</param>
    /// <param name="isDeviceLog">Если true, логируется в файл для оборудования.</param>
    /// <param name="callerFilePath">Путь к исходному файлу, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="lineNumber">Номер строки, откуда вызван метод. Заполняется автоматически.</param>
    /// <returns>Исходное сообщение.</returns>
    public static string LogDebug(string message, bool isDeviceLog = false, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int lineNumber = 0)
    {
      var logger = LogManager.GetLogger(GetLoggerName(callerFilePath, isDeviceLog));
      logger.Debug(BuildMessage(message, callerFilePath, lineNumber));
      return message;
    }

    /// <summary>
    /// Логирует исключение с возможностью фильтрации трассировки стека.
    /// </summary>
    /// <param name="ex">Исключение для логирования.</param>
    /// <param name="customMessage">Дополнительное сообщение к исключению.</param>
    /// <param name="isDeviceLog">Если true, логируется в файл для оборудования.</param>
    /// <param name="file">Файл, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="line">Номер строки, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="onlyProjectStack">Если true, логируется только часть стека, относящаяся к проекту.</param>
    public static void LogException(Exception ex, string customMessage = null, bool isDeviceLog = false, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, bool onlyProjectStack = false)
    {
      if (ex.Message.Contains("The operation was canceled."))
      {
        return;
      }

      var logger = LogManager.GetLogger(GetLoggerName(file, isDeviceLog));
      var messageCore = string.IsNullOrEmpty(customMessage)
        ? ex.Message
        : $"{customMessage}: {ex.Message}";

      var message = BuildMessage(messageCore, file, line);

      if (!onlyProjectStack)
      {
        logger.Error(ex, message);
        return;
      }

      string[] filteredStack = ex.StackTrace?
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(s => s.Contains("AskMkiM"))
        .ToArray() ?? Array.Empty<string>();

      string filtered = string.Join(Environment.NewLine, filteredStack);

      logger.Error($"{message}{Environment.NewLine}{filtered}");
    }

    /// <summary>
    /// Логирует исключение с сообщением для пользователя и возможностью фильтрации трассировки стека.
    /// </summary>
    /// <param name="userHint">Сообщение для пользователя, поясняющее контекст ошибки.</param>
    /// <param name="ex">Исключение для логирования.</param>
    /// <param name="customMessage">Дополнительное сообщение к исключению.</param>
    /// <param name="isDeviceLog">Если true, логируется в файл для оборудования.</param>
    /// <param name="file">Файл, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="line">Номер строки, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="onlyProjectStack">Если true, логируется только часть стека, относящаяся к проекту.</param>
    public static void LogException(string userHint, Exception ex, string customMessage = null, bool isDeviceLog = false, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, bool onlyProjectStack = false)
    {
      var logger = LogManager.GetLogger(GetLoggerName(file, isDeviceLog));
      if (!string.IsNullOrWhiteSpace(userHint))
      {
        logger.Error(BuildMessage(userHint, file, line));
      }

      LogException(ex, customMessage, isDeviceLog, file, line, onlyProjectStack);
    }

    /// <summary>
    /// Получает имя логгера на основе пути к файлу и типа логирования.
    /// </summary>
    /// <param name="filePath">Полный путь к файлу, откуда был вызван метод.</param>
    /// <param name="isDeviceLog">Если true, используется логгер для оборудования.</param>
    /// <returns>Имя логгера.</returns>
    private static string GetLoggerName(string filePath, bool isDeviceLog)
    {
      var baseName = Path.GetFileNameWithoutExtension(filePath);
      return isDeviceLog ? $"{baseName}_Device" : $"{baseName}_UI";
    }

    private static string BuildMessage(string message, string filePath, int lineNumber)
    {
      var safeMessage = message ?? string.Empty;
      var safePath = string.IsNullOrWhiteSpace(filePath) ? "unknown" : TrimPathToProject(filePath);
      return $"[{safePath}:{lineNumber}] {safeMessage}";
    }

    private static string TrimPathToProject(string filePath)
    {
      const string projectName = "AskMkiM";
      var normalized = filePath.Replace('/', '\\');

      var marker = "\\" + projectName + "\\";
      var index = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
      if (index >= 0)
        return normalized.Substring(index + 1);

      index = normalized.IndexOf(projectName + "\\", StringComparison.OrdinalIgnoreCase);
      if (index >= 0)
        return normalized.Substring(index);

      return normalized;
    }
  }
}
