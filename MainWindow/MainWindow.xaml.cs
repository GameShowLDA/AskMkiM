using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static AppConfig.EventAggregator;
using static AppConfig.SettingsFileReader;
using static Utilities.LoggerUtility;

namespace MainWindowProgram
{
  /// <summary>
  /// Основной класс MainWindow, представляющий главное окно приложения.
  /// </summary>
  /// <remarks>
  /// Этот класс содержит логику инициализации главного окна приложения, а также обработки ошибок и отображения сообщений.
  /// Включает в себя таймер для анимации затухания сообщений, статический экземпляр TextBlock для отображения информации,
  /// и асинхронный метод для начальной конфигурации приложения.
  /// </remarks>
  public partial class MainWindow : Window
  {
    /// <summary>
    /// Таймер для анимации затухания сообщений.
    /// </summary>
    static System.Timers.Timer timer = new System.Timers.Timer();

    /// <summary>
    /// Статический экземпляр TextBlock для отображения информации в InfoBlock.
    /// </summary>
    static TextBlock _infoBlock;

    /// <summary>
    /// Флаг блокировки состояния.
    /// </summary>
    private bool isLocked = false;

    /// <summary>
    /// Обработчик сообщений, использующий TextBlock для отображения информации.
    /// </summary>
    MessageHandler messageHandler = new MessageHandler(infoBlock: _infoBlock);

    /// <summary>
    /// Конструктор MainWindow.
    /// </summary>
    /// <remarks>
    /// Инициализирует компоненты главного окна и запускает асинхронную задачу по чтению настроек конфигурации.
    /// В случае возникновения исключения выводит сообщение об ошибке и логирует её.
    /// </remarks>
    public MainWindow()
    {
      InitializeComponent();

      Task.Run(async () =>
      {
        try
        {
          await StartConfigAsync();
        }
        catch (Exception ex)
        {
          string errorDetails = GetErrorDetails(ex);
          LogError($"Ошибка выполнения программы: {errorDetails}");
          MessageBox.Show($"Ошибка: {errorDetails}");
        }
      });

      SettingsGUI();
    }

    private void SettingsGUI()
    {
      _infoBlock = InfoBlock;
      this.Admin.Visibility = Visibility.Collapsed;
      this.Closing += MainWindow_Closing;

      this.CommandBindings.Add(new CommandBinding(ActivateMenuItemCommand, ExecuteActivateMenuItem));
      this.PreviewKeyDown += MainWindow_PreviewKeyDown;

      LockedChanged += ApplicationDataHandler_LockedChanged;
      AdminRightsChanged += ApplicationDataHandler_AdminRightsChanged;
      LogInformation("Главное окно инициализировано.");
    }

    /// <summary>
    /// Получает детали ошибки из исключения.
    /// </summary>
    /// <param name="ex">Исключение, содержащее информацию об ошибке.</param>
    /// <returns>Строка с деталями ошибки, включая сообщение, файл и строку, где произошла ошибка.</returns>
    private string GetErrorDetails(Exception ex)
    {
      StackTrace trace = new StackTrace(ex, true);
      StackFrame frame = trace.GetFrame(0);
      string fileName = frame?.GetFileName() ?? "Неизвестный файл";
      int lineNumber = frame?.GetFileLineNumber() ?? -1;
      return $"{ex.Message} (Файл: {fileName}, строка: {lineNumber})";
    }

    /// <summary>
    /// Асинхронный метод для начала конфигурации приложения.
    /// </summary>
    /// <remarks>
    /// Выполняет асинхронное чтение всех настроек из файла конфигурации.
    /// </remarks>
    private async Task StartConfigAsync()
    {
      await ReadAllSettingsAsync();
    }
  }
}