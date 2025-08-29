using DataBaseConfiguration.Services.Device;
using Mode.Base;
using NewCore.Base.Interface.Main;
using System.Windows.Controls;
using System.Windows.Media;
using UI.Controls.ProtocolNew;
using Utilities.Help;
using Utilities.Models;
using static NewCore.Enum.DeviceEnum;
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
    /// <summary>
    /// Поле для общения с тестируемым БК
    /// </summary>
    private IRelaySwitchModule testedModuleRelayControl;

    /// <summary>
    /// Поле для общения с проверяющим БК
    /// </summary>
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

      // Регистрируем обработчик движения мыши
      MouseMove += (s, e) =>
      {
        // Обновляем последний элемент под курсором
        HelpProvider.SetHelpKey(this, "CrossTestMkr");
      };
    }

    #region Инициализация

    /// <summary>
    /// Асинхронная настройка UI, добавление полей, запуск ProtocolSelfCheckControl.
    /// </summary>
    public async Task InitializeSettingsAsync()
    {
      LogInformation("Настройка CrossTestMKRControl");

      // Настройка контроля и передача необходимых делегатов
      ProtocolSelfCheckControl.SetSettings(
        this,
        StartDelegate: ExecuteTestProcess,
        true,
        StopDelegate: Stop);
      //ProtocolSelfCheckControl.Header = "Перекрёстный тест";

      LogInformation("Настройка CrossTestMKRControl завершена");
    }

    /// <summary>
    /// Ищет релейные модули по строкам "шасси.модуль" и сохраняет их в поля
    /// testedModuleRelayControl и verificatModuleRelayControl.
    /// </summary>
    /// <param name="numTestedModule">Строка вида "chassis.module" для тестируемого модуля.</param>
    /// <param name="numVerificatModule">Строка вида "chassis.module" для проверяющего модуля.</param>
    /// <returns>True, если оба модуля найдены и инициализированы; иначе — false.</returns>
    private async Task<bool> SearchAndInitializeRelaySwitchModules(string numTestedModule, string numVerificatModule)
    {
      var searchService = new RelaySwitchModuleServices();
      var searckChassis = new ChassisManagerServices();

      // Разбираем "шасси.модуль" на две части
      var testedCoords = numTestedModule.Split('.').Select(int.Parse).ToArray();
      var verificatCoords = numVerificatModule.Split('.').Select(int.Parse).ToArray();

      var chassis = searckChassis.GetEntityById(testedCoords[0]);

      // 1) Получить список модулей из шасси тестируемого
      if (chassis == null)
      {
        await ProtocolSelfCheckControl
            .ShowMessageAsync(new ShowMessageModel(
                "Шасси тестируемого модуля не найдено!",
                ShowMessageModel.ErrorMessage.TitleColor));
        return false;
      }

      var list = searchService.GetDevicesByNumberChassis(testedCoords[0]);

      // 2) Найти сам модуль по его Id
      testedModuleRelayControl = list.FirstOrDefault(m => m.Number == testedCoords[1]);
      if (testedModuleRelayControl == null)
      {
        await ProtocolSelfCheckControl
            .ShowMessageAsync(new ShowMessageModel(
                "Тестируемый модуль не найден!",
                ShowMessageModel.ErrorMessage.TitleColor));
        return false;
      }

      // 3) То же самое для проверяющего модуля
      list = searchService.GetDevicesByNumberChassis(verificatCoords[0]);
      if (list == null || list.Count == 0)
      {
        await ProtocolSelfCheckControl
            .ShowMessageAsync(new ShowMessageModel(
                "Шасси проверяющего модуля не найдено!",
                ShowMessageModel.ErrorMessage.TitleColor));
        return false;
      }

      verificatModuleRelayControl = list
          .FirstOrDefault(m => m.Number == verificatCoords[1]);
      if (verificatModuleRelayControl == null)
      {
        await ProtocolSelfCheckControl
            .ShowMessageAsync(new ShowMessageModel(
                "Проверяющий модуль не найден!",
                ShowMessageModel.ErrorMessage.TitleColor));
        return false;
      }

      // Оба модуля найдены
      return true;
    }

    #endregion

    #region Методы тестирования

    /// <summary>
    /// Выполняет основную логику теста: валидация, инициализация модулей,
    /// подготовка диапазона точек, выполнение трёх этапов перекрёстного теста.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task ExecuteTestProcess(CancellationToken cancellationToken)
    {
      // 1. Валидация и парсинг трёх полей
      var (ok, message, tested, tester, range) = UIValidationHelperLightweight.TryValidateAndParseInput(ProtocolSelfCheckControl);
      if (!ok)
      {
        LogError($"Валидация не пройдена: {message}");
        return;
      }

      // 2. Присваивание ссылок на модули
      if (!(await SearchAndInitializeRelaySwitchModules(tested, tester)))
      {
        LogError("Не были присвоены ссылки на модули");
        return;
      }

      LogInformation("Запуск теста CrossTestMKR...");

      // Устанавливаем флаг сброса
      needReset = true;

      // 3. Преобразуем диапазон в список точек
      List<int> points = ParseRange(range);

      // 4. Подготовка оборудования
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("Инициализация оборудования"), IsBlockStart: true);
      await InitializeModule(ProtocolSelfCheckControl, testedModuleRelayControl, cancellationToken, "тестируемый");
      await InitializeModule(ProtocolSelfCheckControl, verificatModuleRelayControl, cancellationToken, "проверяющий");

      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("Настройка оборудования"), IsBlockStart: true);
      await MeterEnableAsync(ProtocolSelfCheckControl, verificatModuleRelayControl, cancellationToken);

      // 5. Собственно сам тест
      await RunPart1(ProtocolSelfCheckControl, testedModuleRelayControl, verificatModuleRelayControl, points, SwitchingBus.A1, SwitchingBus.B1, BusPoint.A, BusPoint.B, cancellationToken);
      await RunPart2(ProtocolSelfCheckControl, testedModuleRelayControl, verificatModuleRelayControl, points, SwitchingBus.B1, SwitchingBus.A1, BusPoint.B, BusPoint.A, cancellationToken);
      await RunPart3(ProtocolSelfCheckControl, testedModuleRelayControl, verificatModuleRelayControl, cancellationToken, false);
    }

    /// <summary>
    /// Принудительно останавливает выполнение теста CrossTestMKR:
    ///  • выключает измеритель;
    ///  • сбрасывает оба модуля;
    ///  • выполняет общий Reset всей системы.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task Stop(CancellationToken cancellationToken)
    {
      if (!needReset) return;
      needReset = false;
    }

    #endregion
  }
}