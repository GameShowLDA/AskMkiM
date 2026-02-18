using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using ICSharpCode.AvalonEdit.Document;

namespace UI.Controls.TextEditor
{
  /// <summary>
  /// Адаптер документа AvalonEdit к абстрактной модели текстового документа.
  ///
  /// Представляет слой изоляции между Core-логикой приложения и конкретной
  /// библиотекой редактора (AvalonEdit). Преобразует TextDocument в контракт
  /// <see cref="ITextDocumentView"/>, используемый остальными подсистемами:
  /// анализаторами, транслятором, поиском, защитой диапазонов и т.д.
  ///
  /// Гарантирует:
  ///  • независимость Core от AvalonEdit
  ///  • единые координаты (offset/line/column)
  ///  • отслеживаемые диапазоны (anchor)
  ///  • уведомления об изменении текста
  ///
  /// Все координаты соответствуют контракту ITextDocumentView:
  ///  offset — 0-based
  ///  line   — 1-based
  ///  column — 0-based
  /// </summary>
  public sealed class AvalonTextDocumentAdapter : ITextDocumentView
  {
    /// <summary>
    /// Доступ к исходному документу AvalonEdit.
    /// Предназначен исключительно для UI-слоя.
    /// Использование вне UI нарушает слой изоляции.
    /// </summary>
    private readonly TextDocument _document;

    /// <summary>
    /// Инициализирует адаптер для указанного документа AvalonEdit.
    /// Подписывается на события изменения текста для проброса в Core.
    /// </summary>
    public AvalonTextDocumentAdapter(TextDocument document)
    {
      _document = document ?? throw new ArgumentNullException(nameof(document));
      _document.Changed += OnDocumentChanged;
    }

    /// <summary>
    /// Даёт доступ UI-слою к реальному документу.
    /// Core этого никогда не видит.
    /// </summary>
    internal TextDocument InnerDocument => _document;

    /// <summary>
    /// Длина полного текста документа.
    /// Установка значения полностью заменяет содержимое.
    /// </summary>
    public int TextLength => _document.TextLength;

    /// <summary>
    /// Полный текст документа.
    /// Установка значения полностью заменяет содержимое.
    /// </summary>
    public string Text
    {
      get => _document.Text;
      set => _document.Text = value;
    }

    /// <summary>
    /// Количество строк в документе (минимум 1).
    /// </summary>
    public int LineCount => _document.LineCount;

    /// <summary>
    /// Событие изменения содержимого документа.
    /// Возникает при любом редактировании текста.
    /// </summary>
    public event EventHandler TextChanged;

    private void OnDocumentChanged(object sender, DocumentChangeEventArgs e)
    {
      TextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Возвращает строку по номеру (1-based).
    /// </summary>
    public TextLine GetLine(int number)
    {
      var line = _document.GetLineByNumber(number);
      return new TextLine(number, line.Offset, line.Length);
    }

    /// <summary>
    /// Возвращает текст указанной строки.
    /// </summary>
    public string GetText(TextLine line)
    {
      return _document.GetText(line.Offset, line.Length);
    }

    /// <summary>
    /// Преобразует координаты (строка, колонка) в абсолютный offset.
    /// </summary>
    public int GetOffset(int line, int column) => _document.GetOffset(line, column);

    /// <summary>
    /// Заменяет диапазон текста.
    /// Диапазон автоматически обновляет связанные anchor-сегменты.
    /// </summary>
    public void Replace(int offset, int length, string newText)
    {
      _document.Replace(offset, length, newText);
    }

    /// <summary>
    /// Возвращает строку, содержащую указанный offset.
    /// </summary>
    public TextLine GetLineByOffset(int offset)
    {
      var line = _document.GetLineByOffset(offset);

      return new TextLine(
          line.LineNumber,
          line.Offset,
          line.Length
      );
    }

    /// <summary>
    /// Создаёт отслеживаемый диапазон текста.
    /// Диапазон автоматически смещается при редактировании документа.
    /// </summary>
    public ITextSegment CreateAnchor(int offset, int length)
    {
      var start = _document.CreateAnchor(offset);
      var end = _document.CreateAnchor(offset + length);

      return new AvalonAnchorSegment(start, end);
    }

    /// <summary>
    /// Перечисление всех строк документа.
    /// Используется анализаторами и подсистемами разметки.
    /// </summary>
    public IEnumerable<TextLine> Lines
    {
      get
      {
        for (int i = 1; i <= _document.LineCount; i++)
        {
          var line = _document.GetLineByNumber(i);
          yield return new TextLine(line.LineNumber, line.Offset, line.Length);
        }
      }
    }

  }
}
