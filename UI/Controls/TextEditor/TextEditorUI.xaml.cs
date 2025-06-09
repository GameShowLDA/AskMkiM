using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppConfiguration.Interface;
using ControlCommandAnalyser.Parsing;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using NLog;
using UI.Components;
using UI.Components.SearchControls;
using static Utilities.LoggerUtility;

namespace UI.Controls.TextEditor
{
  /// <summary>
  /// Логика взаимодействия для TextEditorUI.xaml.
  /// </summary>
  public partial class TextEditorUI : UserControl, ITextAdapter
  {
    /// <summary>
    /// Экземпляр <see cref="MultiEditorControl"/>, используемый для работы с вкладками редактора.
    /// </summary>
    MultiEditorControl _multiEditorControl;
    private TextMarkerService _markerService;
    private List<string> _pendingHighlights = new();
    private Color backgroudColor = (Color)ColorConverter.ConvertFromString("#b23a48");

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
      set => textEditor.Text = value;
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
    /// Инициализирует новый экземпляр класса <see cref="TextEditorUI"/>.
    /// </summary>
    /// <remarks>
    /// Этот конструктор вызывается при создании экземпляра класса. Он инициализирует компоненты UI и подготавливает текстовый редактор для работы.
    /// </remarks>
    public TextEditorUI()
    {
      InitializeComponent();

      Loaded += (s, e) =>
      {
        if (_markerService == null)
        {
          _markerService = new TextMarkerService(textEditor);
          textEditor.TextArea.TextView.BackgroundRenderers.Add(_markerService);

          var services = textEditor.TextArea.TextView.Services;
          if (services.GetService(typeof(TextMarkerService)) == null)
          {
            services.AddService(typeof(TextMarkerService), _markerService);
          }

          Console.WriteLine("TextMarkerService зарегистрирован.");
        }
        else
        {
          Console.WriteLine("TextMarkerService уже инициализирован.");
        }
      };
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

      foreach (var text in _pendingHighlights)
      {
        HighlightText(text);
      }

      _pendingHighlights.Clear();
    }

    /// <summary>
    /// Подсвечивает указанный текст, если сервис инициализирован. Иначе — откладывает подсветку.
    /// </summary>
    /// <param name="textToHighlight">Текст, который необходимо подсветить.</param>
    public void HighlightText(string textToHighlight)
    {
      if (string.IsNullOrEmpty(textToHighlight))
      {
        return;
      }  

      if (_markerService == null)
      {
        _pendingHighlights.Add(textToHighlight);
        LogInformation("Подсветка отложена до инициализации.");
        return;
      }

      string fullText = textEditor.Text;
      LogInformation($"Текст в редакторе: {fullText}");

      int index = 0;
      while ((index = fullText.IndexOf(textToHighlight, index, StringComparison.OrdinalIgnoreCase)) >= 0)
      {
        LogInformation($"Найдено '{textToHighlight}' на позиции: {index}");
        _markerService.AddMarker(index, textToHighlight.Length, backgroudColor);
        index += textToHighlight.Length;
      }

      textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    /// <summary>
    /// Подсвечивает набор диапазонов текста.
    /// </summary>
    /// <param name="ranges">Список диапазонов (начало, конец).</param>
    public void HighlightRanges(List<(int start, int end)> ranges)
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
    /// Устанавливает ссылку на объект <see cref="MultiEditorControl"/> для управления файлами в редакторе.
    /// </summary>
    /// <param name="multiEditorControl">
    /// Экземпляр класса <see cref="MultiEditorControl"/>, который будет использоваться для управления редакторами.
    /// </param>
    public void SetMultiEditorControl(MultiEditorControl multiEditorControl)
    {
      _multiEditorControl = multiEditorControl;
    }

    /// <summary>
    /// Очищает все подсветки в тексте.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывает метод <see cref="TextMarkerService.ClearAllMarkers"/> для очистки всех маркеров и подсветки
    /// в текущем текстовом редакторе.
    /// </remarks>
    public void ClearHighlights()
    {
      _markerService.ClearAllMarkers();
    }

    /// <summary>
    /// Получает документ текстового редактора.
    /// </summary>
    /// <value>
    /// Возвращает объект <see cref="TextDocument"/>, который представляет текст, загруженный в редактор.
    /// </value>
    public TextDocument Document => textEditor.Document;

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

    private void textEditor_DragEnter(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        textEditor.Background = (Brush)FindResource("ActiveBorderSolidColorBrush");
        e.Effects = DragDropEffects.Copy; 
      }
      else
      {
        e.Effects = DragDropEffects.None; 
      }
    }

    /// <summary>
    /// Обработчик события DragLeave. Восстанавливает исходный фон редактора.
    /// </summary>
    private void textEditor_DragLeave(object sender, DragEventArgs e)
    {
      textEditor.Background = (Brush)FindResource("PrimarySolidColorBrush");
    }

    /// <summary>
    /// Обработчик события Drop. Загружает содержимое перетаскиваемого файла в редактор.
    /// </summary>
    private void textEditor_Drop(object sender, DragEventArgs e)
    {
      textEditor.Background = (Brush)FindResource("PrimarySolidColorBrush");

      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files.Length > 0)
        {
          string filePath = files[0];
          try
          {
            if (_multiEditorControl == null)
            {
              string content = System.IO.File.ReadAllText(filePath);
              textEditor.Text = content;
            }
            else
            {
              _multiEditorControl.OpenFile(filePath);
            }
          }
          catch (Exception ex)
          {
            MessageBox.Show($"Ошибка при открытии файла: {ex.Message}");
          }
        }
      }
    }

    private void CloseEditor_Click(object sender, RoutedEventArgs e)
    {
      var parent = this.Parent as Panel;
      parent?.Children.Remove(this);
    }

    public void ApplyHighlighting(List<HighlightRange> ranges)
    {
      if (_markerService == null)
        InitializeMarkerService();

      _markerService.ClearAllMarkers();

      foreach (var range in ranges)
      {
        if (range.Line < 0 || range.Length <= 0) continue;

        var line = textEditor.Document.GetLineByNumber(range.Line + 1);
        int offset = line.Offset + range.Start;

        var color = range.Target switch
        {
          HighlightTarget.CommandNumber => Colors.DeepSkyBlue,
          HighlightTarget.Mnemonic => Colors.LightGreen,
          _ => Colors.Transparent
        };

        _markerService.AddStyledMarker(offset, range.Length, color, FontWeights.Bold);
      }

      textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    public string GetText()
    {
      return this.Text;
    }
  }
}
