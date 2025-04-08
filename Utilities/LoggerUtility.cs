using System.IO;
using System.Runtime.CompilerServices;
using NLog;

namespace Utilities
{
  static public class LoggerUtility
  {
    /// <summary>
    /// Логирование информационного сообщения.
    /// </summary>
    /// <param name="message">Сообщение для записи в лог.</param>
    /// <param name="callerClassName">Имя вызывающего класса (автоматически заполняется компилятором).</param>
    public static string LogInformation(string message, [CallerFilePath] string callerFilePath = "")
    {
      var logger = LogManager.GetLogger(GetCallerClassName(callerFilePath));
      logger.Info(message);
      return message;
    }

    /// <summary>
    /// Логирование предупреждающего сообщения.
    /// </summary>
    /// <param name="message">Сообщение для записи в лог.</param>
    /// <param name="callerClassName">Имя вызывающего класса (автоматически заполняется компилятором).</param>
    public static string LogWarning(string message, [CallerFilePath] string callerFilePath = "")
    {
      var logger = LogManager.GetLogger(GetCallerClassName(callerFilePath));
      logger.Warn(message);
      return message;
    }

    /// <summary>
    /// Логирование сообщения об ошибке.
    /// </summary>
    /// <param name="message">Сообщение для записи в лог.</param>
    /// <param name="callerClassName">Имя вызывающего класса (автоматически заполняется компилятором).</param>
    public static string LogError(string message, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int lineNumber = 0)
    {
      var logger = LogManager.GetLogger(GetCallerClassName(callerFilePath));
      logger.Error($"{Path.GetFileName(callerFilePath)}:{lineNumber} — {message}");
      return message;
    }

    /// <summary>
    /// Логирование сообщения об ошибке.
    /// </summary>
    /// <param name="message">Сообщение для записи в лог.</param>
    public static string LogDebug(string message, [CallerFilePath] string callerFilePath = "")
    {
      var logger = LogManager.GetLogger(GetCallerClassName(callerFilePath));
      logger.Debug(message);
      return message;
    }

    /// <summary>
    /// Логирует исключение с полным стеком вызова и указанием места возникновения.
    /// При желании можно отфильтровать стек вызова, оставив только строки, относящиеся к коду проекта.
    /// </summary>
    /// <param name="ex">Исключение, подлежащее логированию.</param>
    /// <param name="customMessage">Дополнительное сообщение для логирования (необязательно).</param>
    /// <param name="file">Путь к исходному файлу, откуда вызван метод (заполняется автоматически).</param>
    /// <param name="line">Номер строки, откуда вызван метод (заполняется автоматически).</param>
    /// <param name="onlyProjectStack">Если true — в лог попадут только строки стека, содержащие "AskMkiM".</param>
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
    /// Логирует исключение с пользовательским сообщением и полным или фильтрованным стеком вызова.
    /// </summary>
    /// <param name="userHint">Комментарий от разработчика, поясняющий контекст ошибки.</param>
    /// <param name="ex">Исключение, подлежащее логированию.</param>
    /// <param name="customMessage">Основное описание ситуации (по умолчанию null).</param>
    /// <param name="file">Путь к исходному файлу, откуда вызван метод (заполняется автоматически).</param>
    /// <param name="line">Номер строки, откуда вызван метод (заполняется автоматически).</param>
    /// <param name="onlyProjectStack">Если true — в лог попадут только строки, содержащие "AskMkiM".</param>
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
    /// Получает имя вызывающего класса из пути к файлу.
    /// </summary>
    /// <param name="filePath">Путь к файлу вызывающего класса.</param>
    /// <returns>Имя вызывающего класса.</returns>
    private static string GetCallerClassName(string filePath)
    {
      return Path.GetFileNameWithoutExtension(filePath);
    }

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

    public static void ForceInit()
    {
      // ничего не делает, просто чтобы static ctor сработал
    }


  }
}
