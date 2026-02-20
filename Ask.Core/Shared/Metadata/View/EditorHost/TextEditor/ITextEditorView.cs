using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Annotations;
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
    public UserControl View { get; }

    /// <summary>
    /// Документ редактора, предоставляющий абстрактный доступ к тексту.
    /// Не зависит от конкретной реализации редактора.
    /// </summary>
    public ITextDocumentView Document { get; }


    /// <summary>
    /// Возникает при изменении текста документа пользователем или программно.
    /// Используется анализаторами и механизмами синхронизации.
    /// </summary>
    public event EventHandler TextChanged;

    /// <summary>
    /// Тип файла, определяющий поведение редактора
    /// (подсветка, правила анализа, форматирование и т.д.).
    /// </summary>
    public FileType FileType { get; }

    /// <summary>
    /// Связанная модель редактора.
    /// Хранит служебные данные: путь, состояние, параметры отображения.
    /// </summary>
    public TextEditorModel TextEditorModel { get; set; }

    /// <summary>
    /// Полный текст документа.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Устанавливает, является ли текстовый редактор доступным только для чтения.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Установка разрешенных строк, где можно ставить точки остановки, и вытаскивание данных об этом.
    /// </summary>
    public List<int> RightBreakpoint { get; set; }

    /// <summary>
    /// Лист номеров команд, на которых установлены точки остановки.
    /// </summary>
    public List<int> BreakpointCommandsNumbers { get; }

    /// <summary>
    /// Установить маркер на указанную строку, очищая остальные.
    /// </summary>
    public void SetActiveLine(int lineNumber);

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TextMarkerService"/>.
    /// </summary>
    public void InitializeMarkerService();

    /// <summary>
    /// Включает или отключает интерактивность и видимость точек остановки.
    /// </summary>
    public void ConfigureBreakpoints(bool interactive, bool visible);

    /// <summary>
    /// Устанавливает или снимает точку остановки.
    /// </summary>
    public void EnsureBreakpoint(int formattedLine, int commandNumber, bool isSet, bool raiseEvents = false);

    /// <summary>
    /// Включает точку остановки, делая её активной для отладки и взаимодействия.
    /// </summary>
    public void EnableBreakpoint(int commandNumber, bool raiseEvents = false);

    /// <summary>
    /// Выключает точку остановки, делая её неактивной для отладки и взаимодействия.
    /// </summary>
    public void DisableBreakpoint(int commandNumber, bool raiseEvents = false);

    /// <summary>
    /// Подсвечивает набор диапазонов текста.
    /// </summary>
    /// <param name="ranges">Список диапазонов (начало, конец).</param>
    public void HighlightRanges(IReadOnlyList<(int start, int end)> ranges);

    /// <summary>
    /// Переходит к указанной строке, разворачивает folding при необходимости
    /// и прокручивает редактор так, чтобы строка была видна.
    /// </summary>
    /// <param name="lineNumber">Номер строки (1-based).</param>
    public void GoToLine(int lineNumber);

    /// <summary>
    /// Прокручивает редактор до указанной строки.
    /// </summary>
    /// <param name="line">
    /// Номер строки, до которой нужно прокрутить текст в редакторе.
    /// </param>
    public void ScrollToLine(int line);

    /// <summary>
    /// Выделяет текст в редакторе, начиная с указанного смещения и заданной длины.
    /// </summary>
    /// <param name="startOffset">
    /// Смещение в документе, с которого начинается выделение.
    /// </param>
    /// <param name="length">
    /// Длина выделяемого текста.
    /// </param>
    public void Select(int startOffset, int length);
  }
}
