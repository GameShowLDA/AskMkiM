namespace Ask.Core.Shared.DTO.TextEditor
{
  namespace Ask.Core.Shared.DTO.TextEditor
  {
    /// <summary>
    /// Значимый объект (value object), представляющий строку текстового документа.
    ///
    /// Используется в абстрактной модели документа (<see cref="ITextDocumentView"/>)
    /// и не зависит от конкретной реализации редактора.
    ///
    /// Контракт:
    ///  • LineNumber — номер строки (1-based)
    ///  • Offset     — абсолютная позиция начала строки (0-based)
    ///  • Length     — длина строки без символов перевода строки
    ///
    /// Экземпляр является неизменяемым и не содержит ссылок на UI-компоненты.
    /// </summary>
    public readonly struct TextLine
    {
      /// <summary>
      /// Номер строки в документе (1-based).
      /// </summary>
      public int LineNumber { get; }

      /// <summary>
      /// Абсолютный offset начала строки в документе (0-based).
      /// </summary>
      public int Offset { get; }

      /// <summary>
      /// Длина строки в символах (без учёта перевода строки).
      /// </summary>
      public int Length { get; }

      /// <summary>
      /// Создаёт описание строки документа.
      /// </summary>
      /// <param name="lineNumber">Номер строки (1-based).</param>
      /// <param name="offset">Начальный offset строки (0-based).</param>
      /// <param name="length">Длина строки без перевода строки.</param>
      public TextLine(int lineNumber, int offset, int length)
      {
        LineNumber = lineNumber;
        Offset = offset;
        Length = length;
      }

      /// <summary>
      /// Конечный offset строки (Offset + Length).
      /// </summary>
      public int EndOffset => Offset + Length;
    }
  }

}
