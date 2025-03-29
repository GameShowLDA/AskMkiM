using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;
using UI.Components;

namespace UI.Controls.TextEditor
{
  /// <summary>
  /// Логика взаимодействия для TextEditorUI.xaml.
  /// </summary>
  public partial class TextEditorUI : UserControl
  {
    /// <summary>
    /// Экземпляр <see cref="MultiEditorControl"/>, используемый для работы с вкладками редактора.
    /// </summary>
    MultiEditorControl _multiEditorControl;

    private TextMarkerService _markerService;
    private readonly List<string> _pendingHighlights = new();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TextEditorUI"/>.
    /// </summary>
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

          Console.WriteLine("✅ TextMarkerService зарегистрирован.");
        }
        else
        {
          Console.WriteLine("⚠ TextMarkerService уже инициализирован.");
        }
      };
    }

    /// <summary>
    /// Подсвечивает набор диапазонов текста.
    /// </summary>
    /// <param name="ranges">Список диапазонов (начало, конец).</param>
    public void HighlightRanges(List<(int start, int end)> ranges)
    {
      if (_markerService == null)
      {
        Console.WriteLine("❌ MarkerService не инициализирован. Операция отклонена.");
        return;
      }

      foreach (var (start, end) in ranges)
      {
        if (start >= 0 && end > start && end <= textEditor.Text.Length)
        {
          int length = end - start;
          Console.WriteLine($"Подсветка диапазона: {start}–{end} (длина {length})");
          _markerService.AddMarker(start, length, Colors.OrangeRed);
        }
        else
        {
          Console.WriteLine($"⚠ Некорректный диапазон: ({start}, {end})");
        }
      }

      textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    /// <summary>
    /// Явно инициализирует TextMarkerService. Нужно вызывать вручную после загрузки компонента.
    /// </summary>
    public void InitializeMarkerService()
    {
      if (textEditor == null)
      {
        Console.WriteLine("❌ textEditor == null");
        return;
      }

      if (textEditor.Document == null)
      {
        Console.WriteLine("⚠ textEditor.Document == null. Создаю новый документ.");
        textEditor.Document = new ICSharpCode.AvalonEdit.Document.TextDocument();
      }

      _markerService = new TextMarkerService(textEditor);
      textEditor.TextArea.TextView.BackgroundRenderers.Add(_markerService);
      textEditor.TextArea.TextView.Services.AddService(typeof(TextMarkerService), _markerService);

      Console.WriteLine("✅ TextMarkerService инициализирован.");

      foreach (var text in _pendingHighlights)
      {
        HighlightText(text);
      }

      _pendingHighlights.Clear();
    }

    /// <summary>
    /// Устанавливает ссылку на <see cref="MultiEditorControl"/> для управления файлами.
    /// </summary>
    /// <param name="multiEditorControl">Экземпляр <see cref="MultiEditorControl"/>.</param>
    public void SetMultiEditorControl(MultiEditorControl multiEditorControl)
    {
      _multiEditorControl = multiEditorControl;
    }

    public void HighlightText(string textToHighlight)
    {
      if (string.IsNullOrEmpty(textToHighlight))
      {
        return;
      }

      if (_markerService == null)
      {
        _pendingHighlights.Add(textToHighlight);
        Console.WriteLine("Подсветка отложена до инициализации.");
        return;
      }

      string fullText = textEditor.Text;
      Console.WriteLine($"Текст в редакторе: {fullText}");

      int index = 0;
      while ((index = fullText.IndexOf(textToHighlight, index, StringComparison.OrdinalIgnoreCase)) >= 0)
      {
        Console.WriteLine($"Найдено '{textToHighlight}' на позиции: {index}");
        _markerService.AddMarker(index, textToHighlight.Length, Colors.Red);
        index += textToHighlight.Length;
      }

      textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    /// <summary>
    /// Обработчик события DragEnter. Меняет фон текстового редактора при наведении файла.
    /// </summary>
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

    /// <summary>
    /// Получает или задает текст в текстовом редакторе.
    /// </summary>
    public string Text { get { return textEditor.Text; } set { textEditor.Text = value; } }
  }
}
