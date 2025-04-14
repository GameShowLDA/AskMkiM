using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// Главная ViewModel для основного окна приложения.
  /// Содержит ссылки на все функциональные разделы: метрология, тесты, файлы, настройки, администрирование и окно.
  /// </summary>
  public class MainWindowViewModel
  {
    /// <summary>
    /// ViewModel для раздела метрологии.
    /// </summary>
    public MetrologyViewModel Metrology { get; }

    /// <summary>
    /// ViewModel для работы с тестами.
    /// </summary>
    public TestViewModel Test { get; }

    /// <summary>
    /// ViewModel для управления файлами.
    /// </summary>
    public FileViewModel File { get; }

    /// <summary>
    /// ViewModel для настроек приложения.
    /// </summary>
    public SettingsViewModel Settings { get; }

    /// <summary>
    /// ViewModel для административных функций.
    /// </summary>
    public AdminViewModel Admin { get; }

    /// <summary>
    /// ViewModel для управления состоянием окна (размер, закрытие, перемещение и т.д.).
    /// </summary>
    public WindowViewModel Window { get; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MainWindowViewModel"/>, создавая все дочерние ViewModel.
    /// </summary>
    /// <param name="metrologyService">Сервис для метрологических режимов.</param>
    /// <param name="fileService">Сервис для работы с файлами.</param>
    /// <param name="testService">Сервис для проведения тестов.</param>
    /// <param name="settingsService">Сервис для конфигурации и настроек.</param>
    /// <param name="adminServices">Сервис административных функций.</param>
    /// <param name="window">Сервис для управления главным окном.</param>
    public MainWindowViewModel(
      MetrologyService metrologyService,
      FileService fileService,
      TestService testService,
      SettingsService settingsService,
      AdminServices adminServices,
      WindowService window)
    {
      Metrology = new MetrologyViewModel(metrologyService);
      Test = new TestViewModel(testService);
      File = new FileViewModel(fileService);
      Settings = new SettingsViewModel(settingsService);
      Admin = new AdminViewModel(adminServices);
      Window = new WindowViewModel(window);
    }
  }
}
