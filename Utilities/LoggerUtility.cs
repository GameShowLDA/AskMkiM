using System.IO;
using System.Runtime.CompilerServices;
using NLog;

namespace Utilities
{
  /// <summary>
  /// Утилита для логирования сообщений с использованием NLog.
  /// Предоставляет методы для логирования информации, предупреждений, ошибок, отладочных сообщений и исключений,
  /// включая автоматическое определение вызывающего файла и строки.
  /// </summary>
  static public class LoggerUtility
  {
    /// <summary>
    /// Логирует информационное сообщение.
    /// </summary>
    /// <param name="message">Сообщение для логирования.</param>
    /// <param name="callerFilePath">Путь к исходному файлу, откуда вызван метод. Заполняется автоматически.</param>
    /// <returns>Исходное сообщение.</returns>
    public static string LogInformation(string message, [CallerFilePath] string callerFilePath = "")
    {
      var logger = LogManager.GetLogger(GetCallerClassName(callerFilePath));
      logger.Info(message);
      return message;
    }

    /// <summary>
    /// Логирует предупреждение.
    /// </summary>
    /// <param name="message">Сообщение для логирования.</param>
    /// <param name="callerFilePath">Путь к исходному файлу, откуда вызван метод. Заполняется автоматически.</param>
    /// <returns>Исходное сообщение.</returns>
    public static string LogWarning(string message, [CallerFilePath] string callerFilePath = "")
    {
      var logger = LogManager.GetLogger(GetCallerClassName(callerFilePath));
      logger.Warn(message);
      return message;
    }

    /// <summary>
    /// Логирует сообщение об ошибке, включая имя файла и номер строки.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="callerFilePath">Путь к исходному файлу, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="lineNumber">Номер строки, откуда вызван метод. Заполняется автоматически.</param>
    /// <returns>Исходное сообщение.</returns>
    public static string LogError(string message, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int lineNumber = 0)
    {
      var logger = LogManager.GetLogger(GetCallerClassName(callerFilePath));
      logger.Error($"{Path.GetFileName(callerFilePath)}:{lineNumber} — {message}");
      return message;
    }

    /// <summary>
    /// Логирует отладочное сообщение.
    /// </summary>
    /// <param name="message">Сообщение для логирования.</param>
    /// <param name="callerFilePath">Путь к исходному файлу, откуда вызван метод. Заполняется автоматически.</param>
    /// <returns>Исходное сообщение.</returns>
    public static string LogDebug(string message, [CallerFilePath] string callerFilePath = "")
    {
      var logger = LogManager.GetLogger(GetCallerClassName(callerFilePath));
      logger.Debug(message);
      return message;
    }

    /// <summary>
    /// Логирует исключение с возможностью фильтрации трассировки стека.
    /// </summary>
    /// <param name="ex">Исключение для логирования.</param>
    /// <param name="customMessage">Дополнительное сообщение к исключению.</param>
    /// <param name="file">Файл, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="line">Номер строки, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="onlyProjectStack">Если true, логируется только часть стека, относящаяся к проекту.</param>
    public static void LogException(Exception ex, string customMessage = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, bool onlyProjectStack = false)
    {
      var logger = LogManager.GetLogger(GetCallerClassName(file));
      var fileName = Path.GetFileName(file);

      var message = string.IsNullOrEmpty(customMessage)
        ? $"[{fileName}:{line}] {ex.Message}"
        : $"[{fileName}:{line}] {customMessage}: {ex.Message}";

      if (!onlyProjectStack)
      {
        logger.Error(ex, message); // обычный полный стек
        return;
      }

      // Вырезаем только строки, относящиеся к коду проекта
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
    /// <param name="file">Файл, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="line">Номер строки, откуда вызван метод. Заполняется автоматически.</param>
    /// <param name="onlyProjectStack">Если true, логируется только часть стека, относящаяся к проекту.</param>
    public static void LogException(string userHint, Exception ex, string customMessage = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, bool onlyProjectStack = false)
    {
      var logger = LogManager.GetLogger(GetCallerClassName(file));
      var fileName = Path.GetFileName(file);

      if (!string.IsNullOrWhiteSpace(userHint))
      {
        logger.Error($"[{fileName}:{line}] {userHint}");
      }

      LogException(ex, customMessage, file, line, onlyProjectStack);
    }

    /// <summary>
    /// Получает имя класса, вызвавшего метод, на основе пути к исходному файлу.
    /// </summary>
    /// <param name="filePath">Полный путь к файлу, откуда был вызван метод.</param>
    /// <returns>Имя класса (имя файла без расширения).</returns>
    private static string GetCallerClassName(string filePath)
    {
      return Path.GetFileNameWithoutExtension(filePath);
    }

    /// <summary>
    /// Статический конструктор класса. Загружает конфигурацию NLog из файла и пишет результат в log_debug.txt.
    /// </summary>
    static LoggerUtility()
    {
      try
      {
        LogManager.Setup().LoadConfigurationFromFile("NLog.config");
        LogManager.ReconfigExistingLoggers();

        if (LogManager.Configuration == null)
        {
          File.AppendAllText("log_debug.txt", "NLog.config не загружен\n");
        }
        else
        {
          File.AppendAllText("log_debug.txt", "NLog успешно загружен\n");
        }
      }
      catch (Exception ex)
      {
        File.AppendAllText("log_debug.txt", "Исключение: " + ex.Message);
      }
    }
  }
}
