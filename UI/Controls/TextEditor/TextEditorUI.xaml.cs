using Ask.Core.Services.Config.Base;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.Support;
using Ask.UI.Shared.Contracts;
using Ask.UI.Shared.Contracts.Ask.UI.Shared.Contracts;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using static Ask.LogLib.LoggerUtility;

namespace UI.Controls.TextEditor
{
  /// <summary>
  /// Логика взаимодействия для TextEditorUI.xaml.
  /// </summary>
  public partial class TextEditorUI : UserControl, ITextEditorAdapter, ITextEditorView, IUiViewAdapter
  {

    #region Поля.

    /// <summary>
    /// Отображает визуальные маркеры выполнения (указание активной строки)
    /// в левой области редактора.
    /// </summary>
    private ExecutionGlyphMargin _executionMargin;

    /// <summary>
    /// Менеджер управления сворачиваемыми областями текста в редакторе.
    /// Отвечает за создание, хранение и обновление foldings.
    /// </summary>
    private FoldingManager foldingManager;

    /// <summary>
    /// Стратегия определения областей сворачивания для файлов OPK/OPKW.
    /// Используется для построения структуры foldings.
    /// </summary>
    private OpkwFoldingStrategy foldingStrategy = new OpkwFoldingStrategy();

    /// <summary>
    /// Сервис управления текстовыми маркерами в редакторе.
    /// Используется для подсветки диапазонов и отображения ошибок.
    /// </summary>
    private TextMarkerService _markerService;

    /// <summary>
    /// Список ожидающих подсветок, которые добавляются до момента
    /// полной инициализации сервиса маркеров.
    /// </summary>
    private List<string> _pendingHighlights = new();

    /// <summary>
    /// Цвет фона для подсвечивания выделенных диапазонов текста.
    /// Используется сервисом маркеров.
    /// </summary>
    private Color backgroudColor = (Color)ColorConverter.ConvertFromString("#b23a48");

    private AvalonTextDocumentAdapter _documentAdapter;

    #endregion

    #region Св-ва.

    public UserControl View => this;

    public object NativeView => this;

    /// <summary>
    /// Переопределяет фоновую кисть элемента управления,
    /// перенаправляя получение и установку значения напрямую
    /// в встроенный экземпляр AvalonEdit. Позволяет внешнему коду
    /// изменять фон текстового редактора как у стандартного UI-элемента.
    /// </summary>
    public new Brush Background
    {
      get => textEditor.Background;
      set => textEditor.Background = value;
    }

    /// <summary>
    /// Проксирует событие изменения текста редактора.
    /// Позволяет внешнему коду подписываться на обновления содержимого,
    /// передавая обработчики напрямую во внутренний AvalonEdit.
    /// </summary>
    public event EventHandler TextChanged
    {
      add => textEditor.TextChanged += value;
      remove => textEditor.TextChanged -= value;
    }

    /// <summary>
    /// Определяет тип файла, связанный с данным экземпляром редактора.
    /// Используется для выбора схемы подсветки синтаксиса и других
    /// специфичных для формата настроек. Устанавливается при создании
    /// редактора и доступно только для чтения извне.
    /// </summary>
    public FileType FileType { get; private set; }

    /// <summary>
    /// Модель данных, описывающая состояние и параметры текущего
    /// текстового редактора. Может содержать информацию о файле,
    /// настройках отображения и других связанных данных.
    /// Свойство доступно для чтения и записи.
    /// </summary>
    public TextEditorModel TextEditorModel { get; set; }

    /// <summary>
    /// Получает документ текстового редактора.
    /// </summary>
    /// <value>
    /// Возвращает объект <see cref="TextDocument"/>, который представляет текст, загруженный в редактор.
    /// </value>
    public ITextDocumentView Document => _documentAdapter;

    /// <summary>
    /// Получает экземпляр текстового редактора AvalonEdit.
    /// </summary>
    /// <value>
    /// Возвращает объект <see cref="ICSharpCode.AvalonEdit.TextEditor"/>, который используется в этом классе.
    /// </value>
    public ICSharpCode.AvalonEdit.TextEditor TextEditor => textEditor;

    /// <summary>
    /// Получает или задает текст в текстовом редакторе.
    /// </summary>
    /// <value>
    /// Возвращает или устанавливает строку текста, которая отображается в текстовом редакторе.
    /// </value>
    public string Text
    {
      get => textEditor.Text;
      set
      {
        textEditor.Text = value;

        if (FileType == FileType.OPKW)
        {
          InitializeFolding();
        }
      }
    }

    /// <summary>
    /// Устанавливает, является ли текстовый редактор доступным только для чтения.
    /// </summary>
    public bool IsReadOnly
    {
      get => textEditor.IsReadOnly;
      set => textEditor.IsReadOnly = value;
    }

    /// <summary>
    /// Получает экземпляр сервиса маркеров для подсветки текста в редакторе.
    /// </summary>
    /// <value>
    /// Возвращает объект <see cref="TextMarkerService"/>, который управляет подсветкой текста в редакторе.
    /// Если сервис маркеров ещё не инициализирован, то вызывается его инициализация.
    /// </value>
    public TextMarkerService MarkerService
    {
      get
      {
        if (_markerService == null)
        {
          LogWarning("📢 MarkerService был null, вызываем инициализацию.");
          InitializeMarkerService();
        }

        return _markerService;
      }
    }

    /// <summary>
    /// Установка разрешенных строк, где можно ставить точки остановки, и вытаскивание данных об этом.
    /// </summary>
    public List<int> RightBreakpoint
    {
      get => _executionMargin.RightBreakpoints;
      set => _executionMargin.RightBreakpoints = value;
    }

    /// <summary>
    /// Лист номеров строк, где установлены точки остановки
    /// </summary>
    public List<TextAnchor> BreakPointLines
    {
      get => _executionMargin.BreakpointLines;
    }

    /// <summary>
    /// Лист номеров команд, на которых установлены точки остановки.
    /// </summary>
    public List<int> BreakpointCommandsNumbers
    {
      get => _executionMargin.BreakpointCommandsNumbers;
    }

    #endregion

    /// <summary>
    /// Установить маркер на указанную строку, очищая остальные.
    /// </summary>
    public void SetActiveLine(int lineNumber)
    {
      if (Dispatcher.CheckAccess())
      {
        _executionMargin.SetActiveLine(lineNumber);
        return;
      }

      Dispatcher.Invoke(() => _executionMargin.SetActiveLine(lineNumber));
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TextMarkerService"/>.
    /// </summary>
    public void InitializeMarkerService()
    {
      if (textEditor == null)
      {
        LogError("textEditor == null");
        return;
      }

      if (textEditor.Document == null)
      {
        LogWarning("textEditor.Document == null. Создаю новый документ.");
        textEditor.Document = new ICSharpCode.AvalonEdit.Document.TextDocument();
      }

      _markerService = new TextMarkerService(textEditor);
      textEditor.TextArea.TextView.BackgroundRenderers.Add(_markerService);
      textEditor.TextArea.TextView.Services.AddService(typeof(TextMarkerService), _markerService);

      LogInformation("TextMarkerService инициализирован.");

      _pendingHighlights.Clear();
    }

    public void ConfigureBreakpoints(bool interactive, bool visible)
    {
      _executionMargin.BreakpointsInteractive = interactive;
      _executionMargin.BreakpointsVisible = visible;

      _executionMargin.InvalidateVisual();
    }

    public void EnsureBreakpoint(int formattedLine, int commandNumber, bool isSet, bool raiseEvents = false)
    {
      _executionMargin.EnsureBreakpoint(formattedLine, commandNumber, isSet, raiseEvents);
    }

    /// <summary>
    /// Подсвечивает набор диапазонов текста.
    /// </summary>
    /// <param name="ranges">Список диапазонов (начало, конец).</param>
    public void HighlightRanges(IReadOnlyList<(int start, int end)> ranges)
    {
      if (_markerService == null)
      {
        Console.WriteLine("MarkerService не инициализирован. Операция отклонена.");
        return;
      }

      foreach (var (start, end) in ranges)
      {
        if (start >= 0 && end > start && end <= textEditor.Text.Length)
        {
          int length = end - start;
          Console.WriteLine($"Подсветка диапазона: {start}–{end} (длина {length})");
          _markerService.AddMarker(start, length, backgroudColor);
        }
        else
        {
          Console.WriteLine($"Некорректный диапазон: ({start}, {end})");
        }
      }

      textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    /// <summary>
    /// Переходит к указанной строке, разворачивает folding при необходимости
    /// и прокручивает редактор так, чтобы строка была видна.
    /// </summary>
    /// <param name="lineNumber">Номер строки (1-based).</param>
    public void GoToLine(int lineNumber)
    {
      if (lineNumber > 0 && lineNumber <= textEditor.Document.LineCount)
      {
        var line = textEditor.Document.GetLineByNumber(lineNumber);
        textEditor.ScrollToLine(lineNumber);
        textEditor.Select(line.Offset, line.Length);
        textEditor.Focus();
      }
    }


    /// <summary>
    /// Получает область текста редактора.
    /// </summary>
    /// <value>
    /// Возвращает объект <see cref="TextArea"/>, который представляет текстовую область редактора, включая курсор,
    /// выделение и другие параметры отображения.
    /// </value>
    public TextArea TextArea => textEditor.TextArea;

    /// <summary>
    /// Прокручивает редактор до указанной строки.
    /// </summary>
    /// <param name="line">
    /// Номер строки, до которой нужно прокрутить текст в редакторе.
    /// </param>
    public void ScrollToLine(int line)
    {
      textEditor.ScrollToLine(line);
    }

    /// <summary>
    /// Выделяет текст в редакторе, начиная с указанного смещения и заданной длины.
    /// </summary>
    /// <param name="startOffset">
    /// Смещение в документе, с которого начинается выделение.
    /// </param>
    /// <param name="length">
    /// Длина выделяемого текста.
    /// </param>
    public void Select(int startOffset, int length)
    {
      textEditor.Select(startOffset, length);
    }

    /// <summary>
    /// Обрабатывает вращение колеса мыши в текстовом редакторе.
    /// При удержании клавиши Ctrl выполняет масштабирование текста
    /// (увеличение при прокрутке вверх и уменьшение при прокрутке вниз)
    /// вместо стандартной прокрутки содержимого.
    /// Помечает событие как обработанное.
    /// </summary>
    private void TextEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
      {

        if (e.Delta > 0)
        {
          Zoom(true);
        }
        else if (e.Delta < 0)
        {
          Zoom(false);
        }

        e.Handled = true;
      }
    }

    /// <summary>
    /// Переключает состояние сворачивания блока, в котором находится курсор.
    /// Если курсор расположен внутри области foldings, текущий блок разворачивается
    /// или сворачивается. Если folding-менеджер не инициализирован, метод завершает работу.
    /// </summary>
    private void ToggleCurrentFolding()
    {
      if (foldingManager == null) return;
      int caretOffset = textEditor.CaretOffset;
      foreach (var folding in foldingManager.AllFoldings)
      {
        if (folding.StartOffset <= caretOffset && caretOffset < folding.EndOffset)
        {
          folding.IsFolded = !folding.IsFolded;
          break;
        }
      }
    }

    /// <summary>
    /// Инициализирует менеджер сворачивания текста при необходимости
    /// и обновляет коллекцию foldings согласно установленной стратегии.
    /// Вызывается при загрузке документа или изменении содержимого.
    /// </summary>
    private void InitializeFolding()
    {
      if (foldingManager == null)
        foldingManager = FoldingManager.Install(textEditor.TextArea);

      foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
      ReplaceLineNumberMargin();
      ReplaceFoldingMargin();
    }

    /// <summary>
    /// Применяет или отключает подсветку синтаксиса в текстовом редакторе.
    /// При включении выбирает файл схемы XSHD на основе типа файла редактора,
    /// загружает подсветку и активирует её в AvalonEdit.
    /// При отключении подсветки снимает схему и фиксирует действие в журнале.
    /// Обрабатывает возможные ошибки загрузки XSHD-файла.
    /// </summary>
    private void ApplySyntaxHighlighting(bool enableHighlighting)
    {
      if (!enableHighlighting)
      {
        textEditor.SyntaxHighlighting = null;
        LogDebug("Подсветка отключена пользователем.");
        return;
      }

      string? xshdFile = FileType switch
      {
        FileType.OPK or FileType.OPKW => "MKI_OPKW.xshd",
        FileType.PK or FileType.PKW => "MKI_PK.xshd",
        FileType.Protocol => "MKI_PROTOCOL.xshd",
        _ => null
      };

      if (string.IsNullOrWhiteSpace(xshdFile))
      {
        textEditor.SyntaxHighlighting = null;
        LogDebug($"Для типа файла {FileType} подсветка не назначена.");
        return;
      }

      var xshdPath = ResolveHighlightingPath(xshdFile);
      if (xshdPath == null)
      {
        textEditor.SyntaxHighlighting = null;
        LogWarning($"Файл подсветки не найден: {xshdFile}");
        return;
      }

      try
      {
        using var stream = File.OpenRead(xshdPath);
        using var reader = new XmlTextReader(stream);
        textEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        LogDebug($"Подсветка включена: {textEditor.SyntaxHighlighting?.Name}");
      }
      catch (Exception ex)
      {
        LogError($"Ошибка загрузки подсветки: {ex.Message}");
        textEditor.SyntaxHighlighting = null;
      }
    }

    private static string? ResolveHighlightingPath(string fileName)
    {
      var candidates = new[]
      {
        Path.Combine(AppContext.BaseDirectory, fileName),
        Path.Combine(AppContext.BaseDirectory, "UI", fileName),
        Path.GetFullPath(fileName),
      };

      foreach (var candidate in candidates)
      {
        if (File.Exists(candidate))
        {
          return candidate;
        }
      }

      return null;
    }

    #region Конструкторы

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TextEditorUI"/>.
    /// </summary>
    public TextEditorUI() : this(FileType.None) { }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TextEditorUI"/>.
    /// </summary>
    /// <remarks>
    /// Этот конструктор вызывается при создании экземпляра класса. Он инициализирует компоненты UI и подготавливает текстовый редактор для работы.
    /// </remarks>
    public TextEditorUI(FileType fileType = FileType.None, TextEditorModel textEditorModel = null)
    {
      InitializeComponent();

      FileType = fileType;
      TextEditorModel = textEditorModel;
      _defaultFontSize = textEditor.FontSize;
      EnsureLineNumbersForeground();

      textEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;

      if (_executionMargin == null)
      {
        _executionMargin = new ExecutionGlyphMargin(textEditor);
        textEditor.TextArea.LeftMargins.Insert(0, _executionMargin);
        ReplaceLineNumberMargin();
        ReplaceFoldingMargin();
      }

      if (_markerService == null)
      {
        if (textEditor.Document == null)
          textEditor.Document = new TextDocument();

        _markerService = new TextMarkerService(textEditor);

        var textView = textEditor.TextArea.TextView;
        textView.BackgroundRenderers.Add(_markerService);

        var services = textView.Services;
        if (services.GetService(typeof(TextMarkerService)) == null)
          services.AddService(typeof(TextMarkerService), _markerService);
      }

      Loaded += (s, e) =>
      {
        ApplySyntaxHighlighting(UserInterfaceConfig.GetSyntaxHighlighting());
      };

      HelpProvider.SetHelpKeyProvider(textEditor, () =>
      {
        var sel = textEditor.SelectedText?.Trim();
        return string.IsNullOrWhiteSpace(sel)
          ? "DescriptionWorkTextEditor"
          : sel;
      });

      EventAggregator.Subscribe<ThemeEvent.SyntaxHighlighting>(
        e => ApplySyntaxHighlighting(e.IsEnabled)
      );

      if (textEditor.Document == null)
        textEditor.Document = new TextDocument();

      _documentAdapter = new AvalonTextDocumentAdapter(textEditor.Document);
    }

    /// <summary>
    /// Заменяет стандартный марджин номеров строк на версию
    /// с поддержкой фоновой заливки активного диапазона команды.
    /// </summary>
    private void ReplaceLineNumberMargin()
    {
      var leftMargins = textEditor.TextArea.LeftMargins;

      for (int i = 0; i < leftMargins.Count; i++)
      {
        if (leftMargins[i].GetType().Name == "ActiveRangeLineNumberMargin")
          return;

        if (leftMargins[i] is not LineNumberMargin lineNumberMargin)
          continue;

        var rangeAwareMargin = _executionMargin.CreateRangeAwareLineNumberMargin();
        CopyLineNumberMarginState(lineNumberMargin, rangeAwareMargin);
        BindLineNumberForeground(rangeAwareMargin);
        leftMargins.RemoveAt(i);
        leftMargins.Insert(i, rangeAwareMargin);
        return;
      }
    }

    /// <summary>
    /// Заменяет стандартный марджин сворачивания на версию
    /// с поддержкой фоновой заливки активного диапазона команды.
    /// </summary>
    private void ReplaceFoldingMargin()
    {
      var leftMargins = textEditor.TextArea.LeftMargins;

      for (int i = 0; i < leftMargins.Count; i++)
      {
        if (leftMargins[i].GetType().Name == "ActiveRangeFoldingMargin")
          return;

        if (leftMargins[i] is not FoldingMargin foldingMargin)
          continue;

        var rangeAwareMargin = _executionMargin.CreateRangeAwareFoldingMargin();
        CopyFoldingMarginState(foldingMargin, rangeAwareMargin);

        leftMargins.RemoveAt(i);
        leftMargins.Insert(i, rangeAwareMargin);
        return;
      }
    }

    /// <summary>
    /// Настраивает оттенок номеров строк под тему, если он не задан явно.
    /// </summary>
    private void EnsureLineNumbersForeground()
    {
      object localValue = textEditor.ReadLocalValue(ICSharpCode.AvalonEdit.TextEditor.LineNumbersForegroundProperty);
      if (localValue != DependencyProperty.UnsetValue && textEditor.LineNumbersForeground != null)
        return;

      textEditor.SetResourceReference(
        ICSharpCode.AvalonEdit.TextEditor.LineNumbersForegroundProperty,
        "TextEditorLineNumberBrush");
    }

    /// <summary>
    /// Привязывает foreground кастомного марджина к свойству редактора.
    /// </summary>
    private void BindLineNumberForeground(LineNumberMargin margin)
    {
      BindingOperations.SetBinding(
        margin,
        TextElement.ForegroundProperty,
        new Binding(nameof(ICSharpCode.AvalonEdit.TextEditor.LineNumbersForeground))
        {
          Source = textEditor,
          Mode = BindingMode.OneWay
        });
    }

    /// <summary>
    /// Копирует состояние штатного FoldingMargin в пользовательский,
    /// чтобы сохранить работу glyph-иконок и взаимодействие с FoldingManager.
    /// </summary>
    private static void CopyFoldingMarginState(FoldingMargin source, AbstractMargin target)
    {
      CopyProperty(source, target, "FoldingManager");
      CopyProperty(source, target, "FoldingMarkerBrush");
      CopyProperty(source, target, "SelectedFoldingMarkerBrush");
      CopyProperty(source, target, "SelectedFoldingMarkerBackgroundBrush");
      CopyProperty(source, target, "FoldingControlPen");
    }

    /// <summary>
    /// Копирует визуальное состояние штатного LineNumberMargin в пользовательский.
    /// </summary>
    private static void CopyLineNumberMarginState(LineNumberMargin source, LineNumberMargin target)
    {
      CopyProperty(source, target, "Style");
    }

    /// <summary>
    /// Безопасно копирует свойство через reflection при совпадении типов.
    /// </summary>
    private static void CopyProperty(object source, object target, string propertyName)
    {
      var sourceProp = source.GetType().GetProperty(
        propertyName,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      var targetProp = target.GetType().GetProperty(
        propertyName,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

      if (sourceProp?.CanRead != true || targetProp?.CanWrite != true)
        return;

      if (!targetProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
        return;

      targetProp.SetValue(target, sourceProp.GetValue(source));
    }

    #endregion
  }
}
