using NLog;
using System.Runtime.CompilerServices;

namespace Ask.LogLib
{
  /// <summary>
  /// Уровень сообщения общего журнала приложения.
  /// </summary>
  public enum ApplicationLogLevel
  {
    /// <summary>
    /// Отладочное сообщение.
    /// </summary>
    Debug,

    /// <summary>
    /// Информационное сообщение.
    /// </summary>
    Information,

    /// <summary>
    /// Предупреждение.
    /// </summary>
    Warning,

    /// <summary>
    /// Ошибка.
    /// </summary>
    Error
  }

  /// <summary>
  /// Содержит данные сообщения, записанного в общий журнал приложения.
  /// </summary>
  public sealed class ApplicationLogMessageEventArgs : EventArgs
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ApplicationLogMessageEventArgs"/>.
    /// </summary>
    /// <param name="timestamp">Время записи сообщения.</param>
    /// <param name="level">Уровень сообщения.</param>
    /// <param name="message">Отформатированный текст сообщения.</param>
    /// <param name="isDeviceLog">Признак сообщения журнала оборудования.</param>
    public ApplicationLogMessageEventArgs(
      DateTimeOffset timestamp,
      ApplicationLogLevel level,
      string message,
      bool isDeviceLog)
    {
      Timestamp = timestamp;
      Level = level;
      Message = message;
      IsDeviceLog = isDeviceLog;
    }

    /// <summary>
    /// Время записи сообщения.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Уровень сообщения.
    /// </summary>
    public ApplicationLogLevel Level { get; }

    /// <summary>
    /// Отформатированный текст сообщения.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Признак сообщения журнала оборудования.
    /// </summary>
    public bool IsDeviceLog { get; }
  }

  public sealed class LoggedExceptionEventArgs : EventArgs
  {
    public LoggedExceptionEventArgs(Exception exception, string? customMessage, bool isDeviceLog, string callerFilePath, int lineNumber, bool onlyProjectStack)
    {
      Exception = exception;
      CustomMessage = customMessage;
      IsDeviceLog = isDeviceLog;
      CallerFilePath = callerFilePath;
      LineNumber = lineNumber;
      OnlyProjectStack = onlyProjectStack;
    }

    public Exception Exception { get; }

    public string? CustomMessage { get; }

    public bool IsDeviceLog { get; }

    public string CallerFilePath { get; }

    public int LineNumber { get; }

    public bool OnlyProjectStack { get; }
  }

  static public class LoggerUtility
  {
    private static readonly AsyncLocal<bool> IsNotifyingExceptionLogged = new();

    public static event EventHandler<LoggedExceptionEventArgs>? ExceptionLogged;

    /// <summary>
    /// Возникает после записи сообщения в общий журнал приложения.
    /// </summary>
    public static event EventHandler<ApplicationLogMessageEventArgs>? LogMessageWritten;

    public static Action<LoggedExceptionEventArgs>? ExceptionLoggedCallback { get; set; }

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
      var formattedMessage = BuildMessage(message, callerFilePath, lineNumber);
      logger.Info(formattedMessage);
      NotifyLogMessageWritten(ApplicationLogLevel.Information, formattedMessage, isDeviceLog);
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
      var formattedMessage = BuildMessage(message, callerFilePath, lineNumber);
      logger.Warn(formattedMessage);
      NotifyLogMessageWritten(ApplicationLogLevel.Warning, formattedMessage, isDeviceLog);
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
      var formattedMessage = BuildMessage(message, callerFilePath, lineNumber);
      logger.Error(formattedMessage);
      NotifyLogMessageWritten(ApplicationLogLevel.Error, formattedMessage, isDeviceLog);
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
      var formattedMessage = BuildMessage(message, callerFilePath, lineNumber);
      logger.Debug(formattedMessage);
      NotifyLogMessageWritten(ApplicationLogLevel.Debug, formattedMessage, isDeviceLog);
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
    public static void LogException(Exception ex, string? customMessage = null, bool isDeviceLog = false, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, bool onlyProjectStack = false)
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
        NotifyLogMessageWritten(ApplicationLogLevel.Error, message, isDeviceLog);
        NotifyExceptionLogged(ex, customMessage, isDeviceLog, file, line, onlyProjectStack);
        return;
      }

      string[] filteredStack = ex.StackTrace?
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(s => s.Contains("AskMkiM"))
        .ToArray() ?? Array.Empty<string>();

      string filtered = string.Join(Environment.NewLine, filteredStack);

      logger.Error($"{message}{Environment.NewLine}{filtered}");
      NotifyLogMessageWritten(ApplicationLogLevel.Error, message, isDeviceLog);
      NotifyExceptionLogged(ex, customMessage, isDeviceLog, file, line, onlyProjectStack);
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
    public static void LogException(string userHint, Exception ex, string? customMessage = null, bool isDeviceLog = false, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, bool onlyProjectStack = false)
    {
      var logger = LogManager.GetLogger(GetLoggerName(file, isDeviceLog));
      if (!string.IsNullOrWhiteSpace(userHint))
      {
        var formattedUserHint = BuildMessage(userHint, file, line);
        logger.Error(formattedUserHint);
        NotifyLogMessageWritten(ApplicationLogLevel.Error, formattedUserHint, isDeviceLog);
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

    private static void NotifyExceptionLogged(Exception exception, string? customMessage, bool isDeviceLog, string file, int line, bool onlyProjectStack)
    {
      var handler = ExceptionLogged;
      var callback = ExceptionLoggedCallback;
      if ((handler == null && callback == null) || IsNotifyingExceptionLogged.Value)
      {
        return;
      }

      try
      {
        IsNotifyingExceptionLogged.Value = true;
        var args = new LoggedExceptionEventArgs(exception, customMessage, isDeviceLog, file, line, onlyProjectStack);

        try
        {
          callback?.Invoke(args);
        }
        catch
        {
        }

        if (handler == null)
        {
          return;
        }

        foreach (EventHandler<LoggedExceptionEventArgs> subscriber in handler.GetInvocationList())
        {
          try
          {
            subscriber(null, args);
          }
          catch
          {
          }
        }
      }
      finally
      {
        IsNotifyingExceptionLogged.Value = false;
      }
    }

    /// <summary>
    /// Уведомляет подписчиков о записи сообщения в общий журнал.
    /// </summary>
    /// <param name="level">Уровень сообщения.</param>
    /// <param name="message">Отформатированный текст сообщения.</param>
    /// <param name="isDeviceLog">Признак сообщения журнала оборудования.</param>
    private static void NotifyLogMessageWritten(
      ApplicationLogLevel level,
      string message,
      bool isDeviceLog)
    {
      var handler = LogMessageWritten;
      if (handler == null)
      {
        return;
      }

      var args = new ApplicationLogMessageEventArgs(
        DateTimeOffset.Now,
        level,
        message,
        isDeviceLog);

      foreach (EventHandler<ApplicationLogMessageEventArgs> subscriber in handler.GetInvocationList())
      {
        try
        {
          subscriber(null, args);
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine(
            $"Ошибка подписчика общего журнала: {ex.Message}");
        }
      }
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
