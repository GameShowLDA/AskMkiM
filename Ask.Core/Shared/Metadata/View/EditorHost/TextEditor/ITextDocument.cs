using Ask.Core.Shared.DTO.TextEditor;

namespace Ask.Core.Shared.Metadata.View.EditorHost.TextEditor
{
  /// <summary>
  /// Абстрактное представление текстового документа.
  ///
  /// Является независимой моделью текста, используемой ядром приложения.
  /// Реализация может быть адаптирована к любому редактору (AvalonEdit, Roslyn, Monaco и т.д.).
  ///
  /// Контракт координат:
  ///  • Offset — абсолютная позиция в тексте (0-based)
  ///  • LineNumber — номер строки (1-based)
  ///  • Column — позиция внутри строки (0-based)
  ///
  /// Документ обязан обеспечивать стабильность якорей (anchor),
  /// т.е. диапазоны должны корректно перемещаться при редактировании.
  /// </summary>
  public interface ITextDocumentView
  {
    /// <summary>
    /// Полный текст документа.
    /// Установка значения заменяет всё содержимое.
    /// </summary>
    string Text { get; set; }

    /// <summary>
    /// Общее количество строк в документе.
    /// Минимальное значение — 1 (даже у пустого документа).
    /// </summary>
    int LineCount { get; }

    /// <summary>
    /// Получает строку по номеру (1-based).
    /// </summary>
    TextLine GetLine(int number);

    /// <summary>
    /// Возвращает текст указанной строки.
    /// </summary>
    string GetText(TextLine line);

    /// <summary>
    /// Возникает при любом изменении содержимого документа.
    /// </summary>
    event EventHandler TextChanged;

    /// <summary>
    /// Преобразует координаты (строка, колонка) в абсолютный offset.
    /// </summary>
    int GetOffset(int line, int column);

    /// <summary>
    /// Общая длина текста документа в символах.
    /// </summary>
    int TextLength { get; }

    /// <summary>
    /// Заменяет диапазон текста.
    /// </summary>
    /// <param name="offset">Начальный offset (0-based)</param>
    /// <param name="length">Длина заменяемого диапазона</param>
    /// <param name="newText">Новый текст</param>
    void Replace(int offset, int length, string newText);

    /// <summary>
    /// Получает строку, содержащую указанный offset.
    /// </summary>
    TextLine GetLineByOffset(int offset);

    /// <summary>
    /// Создаёт отслеживаемый диапазон текста (anchor).
    /// Диапазон должен автоматически перемещаться при редактировании документа.
    /// </summary>
    ITextSegment CreateAnchor(int offset, int length);

    /// <summary>
    /// Перечисление всех строк документа в порядке следования.
    /// </summary>
    IEnumerable<TextLine> Lines { get; }
  }
}
