using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Globalization;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Формирует строки протокола самоконтроля АСК в формате старой MKI.
/// </summary>
public sealed class LegacyAskProtocolWriter
{
  private const string HiddenDebugMarker = " ";
  private readonly IUserInteractionService _messageService;

  /// <summary>
  /// Создает writer протокола для вывода строк в новый интерфейс.
  /// </summary>
  /// <param name="messageService">Сервис вывода сообщений в протокол.</param>
  public LegacyAskProtocolWriter(IUserInteractionService messageService)
  {
    _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
  }

  /// <summary>
  /// Записывает начало основного теста.
  /// </summary>
  /// <param name="testName">Название выполняемого теста.</param>
  public Task BeginTestAsync(string testName)
  {
    return WriteCommandAsync($"$TST {testName} {{begin");
  }

  /// <summary>
  /// Записывает завершение основного теста.
  /// </summary>
  /// <param name="testName">Название выполняемого теста.</param>
  public Task EndTestAsync(string testName)
  {
    return WriteLineAsync($"$TST {testName} }}end");
  }

  /// <summary>
  /// Завершает текущую команду в UI.
  /// </summary>
  /// <param name="hasErrors">Признак завершения с ошибкой.</param>
  public Task CompleteCommandAsync(bool hasErrors)
  {
    return _messageService.CompleteCommandAsync(hasErrors);
  }

  /// <summary>
  /// Записывает начало вложенного теста.
  /// </summary>
  /// <param name="number">Номер вложенного теста.</param>
  /// <param name="testTitle">Название основного теста.</param>
  /// <param name="testName">Название вложенного теста.</param>
  public Task BeginSubTestAsync(string testTitle, int number, string testName)
  {
    return WriteCommandAsync($"$TST1 {testTitle} {number}: {testName} {{begin");
  }

  /// <summary>
  /// Записывает завершение вложенного теста.
  /// </summary>
  /// <param name="number">Номер вложенного теста.</param>
  /// <param name="testTitle">Название основного теста.</param>
  /// <param name="testName">Название вложенного теста.</param>
  public Task EndSubTestAsync(string testTitle, int number, string testName)
  {
    return WriteLineAsync($"$TST1 {testTitle} {number}: {testName} }}end");
  }

  /// <summary>
  /// Записывает строку проверки внутри вложенного теста.
  /// </summary>
  /// <param name="message">Текст проверки без префикса <c>$TST2</c>.</param>
  public Task TestStepAsync(string message)
  {
    return WriteLineAsync($"$TST2 {message}");
  }

  /// <summary>
  /// Записывает информационную строку протокола.
  /// </summary>
  /// <param name="message">Текст строки без префикса <c>$DOC</c>.</param>
  public Task DocumentAsync(string message)
  {
    return WriteLineAsync($"$DOC {message}");
  }

  /// <summary>
  /// Записывает строку успешного результата.
  /// </summary>
  /// <param name="message">Текст результата.</param>
  public Task SuccessAsync(string message)
  {
    return WriteLineAsync($"$DOC {message} [НОРМА]");
  }

  /// <summary>
  /// Записывает строку ошибки.
  /// </summary>
  /// <param name="message">Текст ошибки.</param>
  public Task ErrorAsync(string message)
  {
    return WriteLineAsync($"$DOC {message} [БРАК]");
  }

  /// <summary>
  /// Записывает итоговую таблицу старой MKI.
  /// </summary>
  /// <param name="title">Заголовок таблицы.</param>
  /// <param name="isIdleMode">Признак холостого режима.</param>
  /// <param name="startedAt">Время начала выполнения.</param>
  /// <param name="elapsed">Суммарное время выполнения.</param>
  /// <param name="hasErrors">Признак завершения с ошибкой.</param>
  public async Task WriteSummaryAsync(
    string title,
    bool isIdleMode,
    DateTime startedAt,
    TimeSpan elapsed,
    bool hasErrors)
  {
    const int contentWidth = 58;
    string mode = isIdleMode ? "Тест выполнен (холостой режим)" : "Тест выполнен";
    string result = hasErrors ? "Брак" : "Норма";
    string startText = startedAt.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    string totalText = elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    string realText = string.Create(CultureInfo.InvariantCulture, $"{elapsed.TotalSeconds:0.###}с");

    await DocumentAsync(string.Empty);
    await DocumentAsync($"    ┌{CenterTitle(title, contentWidth)}┐");
    await DocumentAsync(BoxLine(mode, contentWidth));
    await DocumentAsync(BoxLine($"Начало выполнения: {startText}", contentWidth));
    await DocumentAsync(BoxLine("  Время выполнения, чч:мм:сс", contentWidth));
    await DocumentAsync(BoxLine($"    └──> Суммарное: {totalText}", contentWidth));
    await DocumentAsync(BoxLine($"    └───> Реальное: {realText}", contentWidth));
    await DocumentAsync(BoxLine($"Результат прогона: {result}", contentWidth));
    await DocumentAsync($"    └{new string('─', contentWidth)}┘");
  }

  /// <summary>
  /// Формирует строку рамки с текстом внутри.
  /// </summary>
  /// <param name="text">Текст строки.</param>
  /// <param name="width">Ширина внутренней части рамки.</param>
  private static string BoxLine(string text, int width)
  {
    return $"    │ {text.PadRight(Math.Max(0, width - 2))} │";
  }

  /// <summary>
  /// Формирует верхнюю линию рамки с заголовком.
  /// </summary>
  /// <param name="title">Заголовок рамки.</param>
  /// <param name="width">Ширина внутренней части рамки.</param>
  private static string CenterTitle(string title, int width)
  {
    string centeredTitle = $" {title} ";
    int left = Math.Max(1, (width - centeredTitle.Length) / 2);
    int right = Math.Max(1, width - centeredTitle.Length - left);
    return new string('─', left) + centeredTitle + new string('─', right);
  }

  /// <summary>
  /// Отправляет командную строку протокола в UI.
  /// </summary>
  /// <param name="line">Полная строка протокола.</param>
  private Task WriteCommandAsync(string line)
  {
    var model = new ShowMessageModel(
      message: line,
      debug: HiddenDebugMarker,
      type: ShowMessageModel.MessageType.Command);

    return _messageService.ShowMessageAsync(model, skipPause: true);
  }

  /// <summary>
  /// Отправляет обычную строку протокола в UI.
  /// </summary>
  /// <param name="line">Полная строка протокола.</param>
  private Task WriteLineAsync(string line)
  {
    var model = new ShowMessageModel(
      message: line,
      debug: HiddenDebugMarker,
      type: ShowMessageModel.MessageType.Info);

    return _messageService.ShowMessageAsync(model, skipPause: true);
  }
}
