using Ask.Core.Services.Config.AppSettings;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System.Windows.Controls;
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
    private readonly List<AnchorSegment> _protected = new();
    private TextDocument _document;

    /// <summary>Переиндексация защищённых участков по документу.</summary>
    public void Rebuild(TextDocument document, IEnumerable<string> readonlyLines)
    {
      _document = document ?? throw new ArgumentNullException(nameof(document));
      _protected.Clear();

      if (readonlyLines == null) return;

      foreach (var line in readonlyLines.Where(s => !string.IsNullOrEmpty(s)))
      {
        foreach (var docLine in _document.Lines)
        {
          string lineText = _document.GetText(docLine);
          if (lineText.Contains(line, StringComparison.Ordinal))
          {
            _protected.Add(new AnchorSegment(_document, docLine.Offset, docLine.TotalLength));
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
