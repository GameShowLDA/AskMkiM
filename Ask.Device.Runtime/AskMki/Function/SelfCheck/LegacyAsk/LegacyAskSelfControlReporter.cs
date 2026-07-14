using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Globalization;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Пишет ход самоконтроля старой АСК в стандартный протокол АСКМ.
/// </summary>
public sealed class LegacyAskSelfControlReporter
{
  private readonly IUserInteractionService _messageService;

  public LegacyAskSelfControlReporter(IUserInteractionService messageService)
  {
    _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
  }

  public Task BeginTestAsync(string testName)
  {
    return WriteAsync(testName, null, ShowMessageModel.MessageType.Command, indentLevel: 0, isCommandHeader: true);
  }

  public Task EndTestAsync(string testName)
  {
    return WriteAsync("Тест завершен", testName, ShowMessageModel.MessageType.Info, indentLevel: 0);
  }

  public Task CompleteCommandAsync(bool hasErrors)
  {
    return _messageService.CompleteCommandAsync(hasErrors);
  }

  public Task BeginSubTestAsync(string testTitle, int number, string testName)
  {
    return WriteAsync($"{number}. {testName}", testTitle, ShowMessageModel.MessageType.CommandBlock, indentLevel: 1);
  }

  public Task EndSubTestAsync(string testTitle, int number, string testName)
  {
    return Task.CompletedTask;
  }

  public Task TestStepAsync(string message)
  {
    return WriteAsync(null, message, ShowMessageModel.MessageType.Info, indentLevel: 2);
  }

  public Task DocumentAsync(string message)
  {
    return WriteAsync(message, null, ShowMessageModel.MessageType.Info, indentLevel: 1);
  }

  public Task SuccessAsync(string message)
  {
    return WriteAsync("Результат", message, ShowMessageModel.MessageType.Success, indentLevel: 1);
  }

  public Task ErrorAsync(string message)
  {
    return WriteAsync("Ошибка", message, ShowMessageModel.MessageType.Error, indentLevel: 1);
  }

  public async Task WriteSummaryAsync(
    string title,
    bool isIdleMode,
    DateTime startedAt,
    TimeSpan elapsed,
    bool hasErrors)
  {
    string mode = isIdleMode ? "Холостой режим" : "Боевой режим";
    string started = startedAt.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    string duration = elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    string result = hasErrors ? "БРАК" : "НОРМА";

    await WriteAsync("Итог самоконтроля", title, hasErrors ? ShowMessageModel.MessageType.Error : ShowMessageModel.MessageType.Success, indentLevel: 0);
    await WriteAsync("Режим", mode, ShowMessageModel.MessageType.Info, indentLevel: 1);
    await WriteAsync("Начало выполнения", started, ShowMessageModel.MessageType.Info, indentLevel: 1);
    await WriteAsync("Время выполнения", duration, ShowMessageModel.MessageType.Info, indentLevel: 1);
    await WriteAsync("Результат", result, hasErrors ? ShowMessageModel.MessageType.Error : ShowMessageModel.MessageType.Success, indentLevel: 1);
  }

  private Task WriteAsync(
    string? header,
    string? message,
    ShowMessageModel.MessageType type,
    int indentLevel,
    bool isCommandHeader = false)
  {
    var model = new ShowMessageModel(header, message: message, type: type)
    {
      IndentLevel = indentLevel,
      IsControlProgramCommandHeader = isCommandHeader
    };

    return _messageService.ShowMessageAsync(model, skipPause: true);
  }
}
