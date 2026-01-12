using Ask.Core.Shared.Interfaces.EventInterfaces;

namespace Ask.Core.Services.EventCore.Events
{
  /// <summary>
  /// Содержит события пользовательского интерфейса, связанные с отображением и очисткой сообщений.
  /// </summary>
  public static class Message
  {
    /// <summary>
    /// Событие, обозначающее очистку блока сообщений в пользовательском интерфейсе.
    /// </summary>
    public class Clear : IEvent { }

    /// <summary>
    /// Событие отображения ошибки в пользовательском интерфейсе.
    /// </summary>
    public class Error : IEvent
    {
      /// <summary>
      /// Текст сообщения об ошибке.
      /// </summary>
      public string Text { get; }

      /// <summary>
      /// Указывает, следует ли очистить предыдущие сообщения перед отображением этого.
      /// </summary>
      public bool ClearPrevious { get; }

      /// <summary>
      /// Инициализирует событие ошибки пользовательского интерфейса.
      /// </summary>
      /// <param name="text">Текст ошибки.</param>
      /// <param name="clearPrevious">
      /// Указывает, следует ли очистить предыдущие сообщения перед отображением этого.
      /// По умолчанию — <see langword="false"/>.
      /// </param>
      public Error(string text, bool clearPrevious = false)
      {
        Text = text;
        ClearPrevious = clearPrevious;
      }
    }

    /// <summary>
    /// Событие отображения предупреждения в пользовательском интерфейсе.
    /// </summary>
    public class Warning : IEvent
    {
      /// <summary>
      /// Текст предупреждения.
      /// </summary>
      public string Text { get; }

      /// <summary>
      /// Указывает, следует ли очистить предыдущие сообщения перед отображением этого.
      /// </summary>
      public bool ClearPrevious { get; }

      /// <summary>
      /// Инициализирует событие предупреждения пользовательского интерфейса.
      /// </summary>
      /// <param name="text">Текст предупреждения.</param>
      /// <param name="clearPrevious">
      /// Указывает, следует ли очистить предыдущие сообщения перед отображением этого.
      /// По умолчанию — <see langword="false"/>.
      /// </param>
      public Warning(string text, bool clearPrevious = false)
      {
        Text = text;
        ClearPrevious = clearPrevious;
      }
    }

    /// <summary>
    /// Событие отображения информационного сообщения в пользовательском интерфейсе.
    /// </summary>
    public class Info : IEvent
    {
      /// <summary>
      /// Текст информационного сообщения.
      /// </summary>
      public string Text { get; }

      /// <summary>
      /// Указывает, следует ли очистить предыдущие сообщения перед отображением этого.
      /// </summary>
      public bool ClearPrevious { get; }

      /// <summary>
      /// Инициализирует событие информационного сообщения пользовательского интерфейса.
      /// </summary>
      /// <param name="text">Текст информационного сообщения.</param>
      /// <param name="clearPrevious">
      /// Указывает, следует ли очистить предыдущие сообщения перед отображением этого.
      /// По умолчанию — <see langword="false"/>.
      /// </param>
      public Info(string text, bool clearPrevious = false)
      {
        Text = text;
        ClearPrevious = clearPrevious;
      }
    }
  }
}
