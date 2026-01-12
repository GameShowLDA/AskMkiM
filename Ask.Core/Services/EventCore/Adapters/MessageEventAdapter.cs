using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;

namespace Ask.Core.Services.EventCore.Adapters
{
  /// <summary>
  /// Статический класс, предоставляющий адаптационный слой между старой системой вызова сообщений
  /// и новой архитектурой событий <see cref="EventCore"/>.
  /// </summary>
  /// <remarks>
  /// Класс сохраняет привычные методы вызова сообщений (<see cref="RaiseErrorMessage"/>,
  /// <see cref="RaiseWarningMessage"/>, <see cref="RaiseInfoMessage"/>) для обратной совместимости.
  /// Внутренне публикация сообщений выполняется через <see cref="EventAggregator"/>,
  /// что обеспечивает типобезопасность, возможность подписки, логирования и фильтрации событий.
  /// </remarks>
  public static class MessageEventAdapter
  {
    /// <summary>
    /// Генерирует событие отображения ошибки в пользовательском интерфейсе.
    /// </summary>
    /// <param name="message">Текст сообщения об ошибке.</param>
    /// <param name="clearPrevious">Указывает, следует ли очистить предыдущие сообщения перед отображением этого.</param>
    /// <example>
    /// <code>
    /// MessageEvent.RaiseErrorMessage("Ошибка при подключении к устройству");
    /// </code>
    /// </example>
    public static void RaiseErrorMessage(string message, bool clearPrevious = false) =>
      EventAggregator.Publish(new Message.Error(message, clearPrevious));

    /// <summary>
    /// Генерирует событие отображения предупреждения в пользовательском интерфейсе.
    /// </summary>
    /// <param name="message">Текст предупреждения.</param>
    /// <param name="clearPrevious">Указывает, следует ли очистить предыдущие сообщения перед отображением этого.</param>
    /// <example>
    /// <code>
    /// MessageEvent.RaiseWarningMessage("Изменения не сохранены", true);
    /// </code>
    /// </example>
    public static void RaiseWarningMessage(string message, bool clearPrevious = false) =>
      EventAggregator.Publish(new Message.Warning(message, clearPrevious));

    /// <summary>
    /// Генерирует событие отображения информационного сообщения в пользовательском интерфейсе.
    /// </summary>
    /// <param name="message">Текст информационного сообщения.</param>
    /// <param name="clearPrevious">Указывает, следует ли очистить предыдущие сообщения перед отображением этого.</param>
    /// <example>
    /// <code>
    /// MessageEvent.RaiseInfoMessage("Операция завершена успешно");
    /// </code>
    /// </example>
    public static void RaiseInfoMessage(string message, bool clearPrevious = false) =>
      EventAggregator.Publish(new Message.Info(message, clearPrevious));

    /// <summary>
    /// Генерирует событие очистки блока сообщений в пользовательском интерфейсе.
    /// </summary>
    public static void RaiseClearMessage() =>
      EventAggregator.Publish(new Message.Clear());
  }
}
