using Mode.Base;
using NewCore.Base.Interface.Main;
using System.Windows.Controls;
using System.Windows.Media;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace Mode.TestSuite.CrossTestMkr
{
  /// <summary>
  /// Контроллер для выполнения перекрёстного теста (CrossTestMKR).
  /// Содержит методы для инициализации, запуска теста и остановки теста.
  /// </summary>
  public partial class CrossTestMkrControl : UserControl
  {
    private IRelaySwitchModule testedModuleRelayControl;
    private IRelaySwitchModule verificatModuleRelayControl;

    /// <summary>
    /// Статусное сообщение для успешного выполнения теста.
    /// </summary>
    private readonly (string Title, Color TitleColor) goodText = SuccessMessage;

    /// <summary>
    /// Статусное сообщение для ошибки в процессе выполнения теста.
    /// </summary>
    private readonly (string Title, Color TitleColor) errorText = ErrorMessage;

    /// <summary>
    /// Флаг, указывающий на необходимость сброса модулей и системы после теста.
    /// </summary>
    private bool needReset = false;

    /// <summary>
    /// Конструктор контроллера, инициализирует UI и вызывает асинхронную настройку.
    /// </summary>
    public CrossTestMkrControl()
    {
      InitializeComponent();
      _ = InitializeSettingsAsync();
    }

    #region Инициализация

    /// <summary>
    /// Асинхронная настройка UI, добавление полей, запуск ProtocolSelfCheckControl.
    /// </summary>
    public async Task InitializeSettingsAsync()
    {
      LogInformation("Настройка CrossTestMKRControl");

      // Настройка контроля и передача необходимых делегатов
      ProtocolSelfCheckControl.SetSettings(this, ExecuteTestProcess, true, Stop);
      ProtocolSelfCheckControl.Header = "Перекрёстный тест";

      LogInformation("Настройка CrossTestMKRControl завершена");
    }

    #endregion

    #region Методы тестирования

    /// <summary>
    /// Метод, который вызывается при нажатии кнопки "Старт".
    /// Выполняет валидацию данных, подготовку оборудования и сам тест.
    /// </summary>
    private async Task ExecuteTestProcess(CancellationToken cancellationToken)
    {
      // 1. Валидация и парсинг трёх полей
      var (ok, message, tested, tester, range) =
          UIValidationHelperLightweight.TryValidateAndParseInput(ProtocolSelfCheckControl);
      if (!ok)
      {
        LogError($"Валидация не пройдена: {message}");
        return;
      }

      LogInformation("Запуск теста CrossTestMKR...");

      // Устанавливаем флаг сброса
      needReset = true;

      // 2. Преобразуем диапазон в список точек
      List<int> points = ParseRange(range);

      // 3. Подготовка оборудования
      await InitializeModule(tested);
      await InitializeModule(tester);
      await MeterEnableAsync(tester);

      // 4. Собственно сам тест
      await RunPart1(tested, tester, points, cancellationToken);
      await RunPart2(tested, tester, points, cancellationToken);
      await RunPart3(tested, tester, points, cancellationToken, false);

      // 5. Отключение измерителя
      await MeterDisableAsync(tester);
    }

    /// <summary>
    /// Принудительно останавливает выполнение теста CrossTestMKR:
    ///  • выключает измеритель;
    ///  • сбрасывает оба модуля;
    ///  • выполняет общий Reset всей системы.
    /// </summary>
    private async Task Stop(CancellationToken cancellationToken)
    {
      if (!needReset) return;

      // Получаем контроллер, чтобы достать текущие значения
      var input = ProtocolSelfCheckControl.GetInputFieldLightweightSafe();
      if (input == null) return;

      // Отключаем измеритель и сбрасываем модули
      await MeterDisableAsync(input.TesterNumber);
      await ResetModule(input.TestedNumber);
      await ResetModule(input.TesterNumber);

      // Сбрасываем флаг необходимости сброса
      needReset = false;
    }

    #endregion
  }
}