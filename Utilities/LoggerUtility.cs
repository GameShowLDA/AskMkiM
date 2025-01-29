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
    public static string LogError(string message, [CallerFilePath] string callerFilePath = "")
    {
      var logger = LogManager.GetLogger(GetCallerClassName(callerFilePath));
      logger.Error(message);
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
      LogManager.Setup().LoadConfigurationFromFile("NLog.config");
      LogManager.ReconfigExistingLoggers();

      if (LogManager.Configuration == null)
      {
        Console.WriteLine("Ошибка: NLog.config не загружен!");
      }
      else
      {
        Console.WriteLine("NLog загружен.");
      }
    }
  }
}
