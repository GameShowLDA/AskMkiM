using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Message;


namespace Mode.Settings.LoggerMessage
{
  /// <summary>
  /// Контрол для отображения логов и работы с лог-файлами.
  /// </summary>
  public partial class LoggerControl : UserControl
  {
    /// <summary>
    /// Флаг, указывающий, был ли инициализирован контрол.
    /// </summary>
    private bool _isInitialized = false;

    /// <summary>
    /// Исходный цвет фона.
    /// </summary>
    private readonly Brush _originalBackground;

    /// <summary>
    /// Цвет фона при активном состоянии.
    /// </summary>
    private readonly Brush _activeBackground;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="LoggerControl"/>.
    /// </summary>
    public LoggerControl()
    {
      InitializeComponent();
      Loaded += LoggerControl_Loaded;
      _originalBackground = BackgroundRichBox.Background;
      _activeBackground = (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"];
    }

    /// <summary>
    /// Обработчик события загрузки контрола.
    /// </summary>
    private void LoggerControl_Loaded(object sender, RoutedEventArgs e)
    {
      if (!_isInitialized)
      {
        InitializeLogger();
      }
    }

    /// <summary>
    /// Инициализация логгера.
    /// </summary>
    private void InitializeLogger()
    {
      if (RequestPinCode())
      {
        LoadLatestLogs();
        _isInitialized = true;
        Visibility = Visibility.Visible; // Делаем контрол видимым после успешного ввода PIN-кода
      }
      else
      {
        Visibility = Visibility.Collapsed;
        MessageBoxCustom.Show("Доступ запрещен. Неверный PIN-код.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Запрашивает PIN-код у пользователя.
    /// </summary>
    /// <returns>True, если PIN-код введён верно; иначе false.</returns>
    private bool RequestPinCode()
    {
      var pinWindow = new PinCodeWindow();
      pinWindow.ShowDialog();
      return pinWindow.IsCorrectPin;
    }

    /// <summary>
    /// Загружает последние логи из файла.
    /// </summary>
    private void LoadLatestLogs()
    {
      string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
      if (!Directory.Exists(logDirectory))
      {
        LogTextBox.Document.Blocks.Add(new Paragraph(new Run("Директория логов не найдена.")));
        return;
      }

      var latestLogFile = Directory.GetFiles(logDirectory, "*.log")
                                   .OrderByDescending(f => new FileInfo(f).CreationTime)
                                   .FirstOrDefault();

      if (latestLogFile == null)
      {
        LogTextBox.Document.Blocks.Add(new Paragraph(new Run("Файлы логов не найдены.")));
        return;
      }

      LoadLogFile(latestLogFile);
    }

    /// <summary>
    /// Обрабатывает предварительное перетаскивание над контролом.
    /// </summary>
    private void LogTextBox_PreviewDragOver(object sender, DragEventArgs e)
    {
      e.Handled = true;
      e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    /// <summary>
    /// Обрабатывает событие перетаскивания файла на контрол.
    /// </summary>
    private void LogTextBox_Drop(object sender, DragEventArgs e)
    {
      BackgroundRichBox.Background = _originalBackground;
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files?.Length > 0)
        {
          string filePath = files[0];
          if (Path.GetExtension(filePath).Equals(".log", StringComparison.OrdinalIgnoreCase))
          {
            LoadLogFile(filePath);
          }
          else
          {
            MessageBoxCustom.Show("Пожалуйста, перетащите файл с расширением .log", "Неверный формат файла", MessageBoxButton.OK, MessageBoxImage.Error);
          }
        }
      }

      e.Handled = true;
    }

    /// <summary>
    /// Загружает указанный лог-файл.
    /// </summary>
    /// <param name="filePath">Путь к файлу лога.</param>
    private void LoadLogFile(string filePath)
    {
      try
      {
        LogTextBox.Document.Blocks.Clear();
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fileStream))
        {
          string line;
          while ((line = reader.ReadLine()) != null)
          {
            ProcessLogLine(line);
          }
        }

        LogTextBox.ScrollToEnd();
        Debug.WriteLine($"Log file loaded successfully: {filePath}");
      }
      catch (IOException ex)
      {
        Debug.WriteLine($"Error loading log file: {ex.Message}");
        LogTextBox.Document.Blocks.Add(new Paragraph(new Run($"Ошибка при чтении файла лога: {ex.Message}")));
      }
    }

    /// <summary>
    /// Обрабатывает строку лога и добавляет её в RichTextBox.
    /// </summary>
    /// <param name="line">Строка лога.</param>
    private void ProcessLogLine(string line)
    {
      var parts = line.Split('\t');
      if (parts.Length >= 6)
      {
        var time = parts[0];
        var level = parts[1];
        var logger = parts[2];
        var message = parts[3];
        var exception = parts[4];
        var marker = parts[5];

        var paragraph = new Paragraph();
        var timeRun = new Run($"[{time}] ") { FontStyle = FontStyles.Italic };
        var loggerRun = new Run($"{logger}: ") { FontWeight = FontWeights.Bold };
        var messageRun = new Run(message);
        var exceptionRun = new Run(exception);
        var markerRun = new Run(marker);

        switch (level)
        {
          case "INFO":
            messageRun.Foreground = Brushes.White;
            break;

          case "WARN":
            messageRun.Foreground = Brushes.Yellow;
            break;

          case "ERROR":
            messageRun.Foreground = Brushes.Red;
            break;

          case "FATAL":
            messageRun.Foreground = Brushes.Red;
            messageRun.Background = Brushes.White;
            break;
        }

        paragraph.Inlines.Add(timeRun);
        paragraph.Inlines.Add(loggerRun);
        paragraph.Inlines.Add(messageRun);

        if (!string.IsNullOrWhiteSpace(exception))
        {
          paragraph.Inlines.Add(new LineBreak());
          paragraph.Inlines.Add(exceptionRun);
        }

        paragraph.Inlines.Add(new Run(" "));
        paragraph.Inlines.Add(markerRun);
        LogTextBox.Document.Blocks.Add(paragraph);
      }
    }

    /// <summary>
    /// Обработчик события входа курсора в область перетаскивания.
    /// </summary>
    private void LogTextBox_PreviewDragEnter(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        BackgroundRichBox.Background = _activeBackground;
      }
    }

    /// <summary>
    /// Обработчик события выхода курсора из области перетаскивания.
    /// </summary>
    private void LogTextBox_PreviewDragLeave(object sender, DragEventArgs e)
    {
      BackgroundRichBox.Background = _originalBackground;
    }
  }
}
