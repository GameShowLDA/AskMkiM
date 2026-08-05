using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Controls.Settings.Protocol
{
  /// <summary>
  /// Контрол для редактирования шаблона протокола.
  /// Загружает базовый шаблон из ProtocolModel и блокирует редактирование ключевых строк.
  /// </summary>
  public partial class ProtocolTemplateEditorControl : UserControl
  {
    public event EventHandler TextChanged;

    // Строки, которые запрещено редактировать
    private readonly string[] _readonlyLines =
    {
      "Протокол($РЕЖИМ) от $ДАТА",
      "проверки электрических параметров сборочной единицы $ОБОЗНАЧЕНИЕ Зав.N $НОМЕР",
      "Программа проверки: $ПРОГРАММА"
    };

    private ProtectedReadOnlySectionProvider _provider;

    public string BaseTemplate { get; private set; }

    /// <summary>
    /// Если true — загружает шаблон с ошибками, если false — обычный.
    /// </summary>
    public bool IsErrorsTemplate { get; set; }

    /// <summary>
    /// Текст шаблона (привязан к ProtocolEditor.Text).
    /// Можно читать и задавать напрямую, поддерживает биндинг.
    /// </summary>
    public string Text
    {
      get => ProtocolEditor.Text;
      set
      {
        if (ProtocolEditor.Text != value)
        {
          ProtocolEditor.Text = value ?? string.Empty;
          _provider.Rebuild(ProtocolEditor.Document, _readonlyLines);
        }
      }
    }

    public new Brush Background
    {
      get
      {
        return ProtocolEditor.Background;
      }
      set
      {
        ProtocolEditor.Background = value;
      }
    }

    public ProtocolTemplateEditorControl()
    {
      InitializeComponent();

      _provider = new ProtectedReadOnlySectionProvider();
      ProtocolEditor.TextArea.ReadOnlySectionProvider = _provider;

      ProtocolEditor.TextChanged += (s, e) =>
      {
        TextChanged?.Invoke(this, EventArgs.Empty);
      };

      Loaded += async (s, e) =>
      {
        if (!string.IsNullOrEmpty(ProtocolEditor.Text))
          return; 

        if (IsErrorsTemplate)
          BaseTemplate = ProtocolConfig.GetBaseTextErrorsProtocol();
        else
          BaseTemplate = ProtocolConfig.GetBaseTextProtocol();

        LoadTemplateWithRequiredLines(BaseTemplate);
      };

    }

    /// <summary>
    /// Передаёт прокрутку родительскому экрану настроек при достижении границы редактора.
    /// </summary>
    /// <param name="sender">Редактор шаблона протокола.</param>
    /// <param name="e">Аргументы события колеса мыши.</param>
    private void ProtocolEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
      {
        return;
      }

      var editor = ProtocolEditor.TextEditor;
      bool reachedTop = e.Delta > 0 && editor.VerticalOffset <= 0;
      bool reachedBottom = e.Delta < 0 &&
        editor.VerticalOffset >= editor.ExtentHeight - editor.ViewportHeight;

      if (!reachedTop && !reachedBottom)
      {
        return;
      }

      var parentScrollViewer = FindVisualParent<ScrollViewer>(this);
      if (parentScrollViewer == null)
      {
        return;
      }

      parentScrollViewer.ScrollToVerticalOffset(parentScrollViewer.VerticalOffset - e.Delta);
      e.Handled = true;
    }

    /// <summary>
    /// Находит ближайший родительский элемент заданного типа в визуальном дереве.
    /// </summary>
    /// <typeparam name="T">Тип родительского элемента.</typeparam>
    /// <param name="element">Начальный элемент поиска.</param>
    /// <returns>Найденный родительский элемент или <see langword="null"/>.</returns>
    private T? FindVisualParent<T>(System.Windows.DependencyObject element)
      where T : System.Windows.DependencyObject
    {
      System.Windows.DependencyObject? current = VisualTreeHelper.GetParent(element);
      while (current != null)
      {
        if (current is T parent)
        {
          return parent;
        }

        current = VisualTreeHelper.GetParent(current);
      }

      return null;
    }

    /// <summary>
    /// Загружает текст в редактор и гарантирует наличие обязательных строк.
    /// </summary>
    private void LoadTemplateWithRequiredLines(string templateText)
    {
      if (string.IsNullOrWhiteSpace(templateText))
        templateText = string.Empty;

      foreach (var line in _readonlyLines)
      {
        if (!templateText.Contains(line, StringComparison.Ordinal))
        {
          templateText += Environment.NewLine + line;
        }
      }

      ProtocolEditor.Text = templateText;
      _provider.Rebuild(ProtocolEditor.Document, _readonlyLines);
    }

    /// <summary>Получить текущий текст шаблона из редактора.</summary>
    public string GetTemplate() => ProtocolEditor.Text;
  }

  /// <summary>
  /// Провайдер «read-only» участков для AvalonEdit:
  /// запрещает удаление/вставку внутри защищённых сегментов.
  /// </summary>
  public sealed class ProtectedReadOnlySectionProvider : IReadOnlySectionProvider
  {
    private readonly List<ITextSegment> _protected = new List<ITextSegment>();
    private ITextDocumentView _document;

    /// <summary>Переиндексация защищённых участков по документу.</summary>
    public void Rebuild(ITextDocumentView document, IEnumerable<string> readonlyLines)
    {
      _document = document ?? throw new ArgumentNullException(nameof(document));
      _protected.Clear();

      if (readonlyLines == null)
        return;

      foreach (var line in readonlyLines.Where(s => !string.IsNullOrEmpty(s)))
      {
        foreach (var docLine in _document.Lines)
        {
          string lineText = _document.GetText(docLine);

          if (lineText.Contains(line, StringComparison.Ordinal))
          {
            _protected.Add(_document.CreateAnchor(docLine.Offset, docLine.Length));
          }
        }
      }
    }

    /// <summary>
    /// Возвращает части запрошенного диапазона, которые МОЖНО удалить.
    /// </summary>
    public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
    {
      if (_document == null || segment == null)
        yield break;

      int start = segment.Offset;
      int end = segment.EndOffset;

      var overlapping = _protected
        .Where(s => s.EndOffset > start && s.Offset < end)
        .OrderBy(s => s.Offset)
        .ToList();

      int cursor = start;

      foreach (var block in overlapping)
      {
        if (block.Offset > cursor)
        {
          yield return new SimpleSegmentCompat(cursor, block.Offset - cursor);
        }
        cursor = Math.Max(cursor, block.EndOffset);
      }

      if (cursor < end)
      {
        yield return new SimpleSegmentCompat(cursor, end - cursor);
      }
    }

    /// <summary>
    /// Разрешать ли вставку по указанной позиции.
    /// </summary>
    public bool CanInsert(int offset)
    {
      return !_protected.Any(s => offset > s.Offset && offset < s.EndOffset);
    }

    /// <summary>
    /// Простой публичный сегмент (замена внутреннему SimpleSegment AvalonEdit).
    /// </summary>
    private sealed class SimpleSegmentCompat : ISegment
    {
      public int Offset { get; }
      public int Length { get; }
      public int EndOffset => Offset + Length;

      public SimpleSegmentCompat(int offset, int length)
      {
        Offset = offset;
        Length = length;
      }
    }
  }
}
