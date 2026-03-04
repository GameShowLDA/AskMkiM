using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using System.Windows.Controls;

namespace Ask.Core.Shared.Metadata.View.EditorHost.TextEditor
{
  /// <summary>
  /// Контракт UI-представления текстового редактора.
  /// 
  /// Интерфейс является адаптационным слоем между ядром приложения и конкретной
  /// реализацией пользовательского интерфейса. Реализация может быть основана на
  /// любой UI-технологии (WPF, Avalonia, WinUI и т.п.), при этом остальная система
  /// взаимодействует только через данный контракт.
  /// 
  /// Предоставляет:
  ///  • доступ к документу
  ///  • навигацию по тексту
  ///  • управление точками остановки
  ///  • подсветку и маркеры
  ///  • базовые операции редактирования
  /// 
  /// Интерфейс не должен раскрывать конкретные типы UI-библиотеки.
  /// </summary>
  public interface ITextEditorView
  {
    /// <summary>
    /// Визуальный элемент редактора.
    /// Используется хостом интерфейса для размещения редактора в layout,
    /// но не должен использоваться логикой ядра напрямую.
    /// </summary>
    UserControl View { get; }

    /// <summary>
    /// Документ редактора, предоставляющий абстрактный доступ к тексту.
    /// Не зависит от конкретной реализации редактора.
    /// </summary>
    ITextDocumentView Document { get; }


    /// <summary>
    /// Возникает при изменении текста документа пользователем или программно.
    /// Используется анализаторами и механизмами синхронизации.
    /// </summary>
    event EventHandler TextChanged;

    /// <summary>
    /// Тип файла, определяющий поведение редактора
    /// (подсветка, правила анализа, форматирование и т.д.).
    /// </summary>
    FileType FileType { get; }

    /// <summary>
    /// Связанная модель редактора.
    /// Хранит служебные данные: путь, состояние, параметры отображения.
    /// </summary>
    TextEditorModel TextEditorModel { get; set; }

    /// <summary>
    /// Полный текст документа.
    /// </summary>
    string Text { get; set; }

    /// <summary>
    /// Устанавливает, является ли текстовый редактор доступным только для чтения.
    /// </summary>
    bool IsReadOnly { get; set; }

    /// <summary>
    /// Установка разрешенных строк, где можно ставить точки остановки, и вытаскивание данных об этом.
    /// </summary>
    List<int> RightBreakpoint { get; set; }

    /// <summary>
    /// Лист номеров команд, на которых установлены точки остановки.
    /// </summary>
    List<int> BreakpointCommandsNumbers { get; }

    /// <summary>
    /// Установить маркер на указанную строку, очищая остальные.
    /// </summary>
    void SetActiveLine(int lineNumber);

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TextMarkerService"/>.
    /// </summary>
    void InitializeMarkerService();

    /// <summary>
    /// Включает или отключает интерактивность и видимость точек остановки.
    /// </summary>
    void ConfigureBreakpoints(bool interactive, bool visible);

    /// <summary>
    /// Устанавливает или снимает точку остановки.
    /// </summary>
    void EnsureBreakpoint(int formattedLine, int commandNumber, bool isSet, bool raiseEvents = false);

    /// <summary>
    /// Подсвечивает набор диапазонов текста.
    /// </summary>
    /// <param name="ranges">Список диапазонов (начало, конец).</param>
    void HighlightRanges(IReadOnlyList<(int start, int end)> ranges);

    /// <summary>
    /// Переходит к указанной строке, разворачивает folding при необходимости
    /// и прокручивает редактор так, чтобы строка была видна.
    /// </summary>
    /// <param name="lineNumber">Номер строки (1-based).</param>
    void GoToLine(int lineNumber);

    /// <summary>
    /// Прокручивает редактор до указанной строки.
    /// </summary>
    /// <param name="line">
    /// Номер строки, до которой нужно прокрутить текст в редакторе.
    /// </param>
    void ScrollToLine(int line);

    /// <summary>
    /// Выделяет текст в редакторе, начиная с указанного смещения и заданной длины.
    /// </summary>
    /// <param name="startOffset">
    /// Смещение в документе, с которого начинается выделение.
    /// </param>
    /// <param name="length">
    /// Длина выделяемого текста.
    /// </param>
    void Select(int startOffset, int length);
  }
}
