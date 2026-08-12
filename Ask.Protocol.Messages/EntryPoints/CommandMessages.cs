using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Protocol.Messages.Builders;
using Ask.Protocol.Messages.Show;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Формирует и выводит сообщения команд программы контроля.
/// </summary>
public static class CommandMessages
{
  /// <summary>
  /// Форматирует исходные строки команды для экранного протокола.
  /// </summary>
  /// <param name="sourceLines">Исходные строки команды программы контроля.</param>
  /// <returns>Строки команды с протокольными отступами или пустая строка.</returns>
  public static string FormatSourceLines(IEnumerable<string> sourceLines)
  {
    ArgumentNullException.ThrowIfNull(sourceLines);
    var lines = sourceLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
    return lines.Count == 0
      ? string.Empty
      : "  " + string.Join("\r\n  ", lines);
  }

  /// <summary>
  /// Форматирует полную исходную команду с заменой её заголовка на отображаемый заголовок вложенного этапа.
  /// </summary>
  /// <param name="displayHeader">Заголовок вложенного этапа.</param>
  /// <param name="sourceLines">Исходные строки команды программы контроля.</param>
  /// <returns>Полный текст команды без повторения исходного номера и мнемоники.</returns>
  public static string FormatSourceLinesWithHeader(
    string displayHeader,
    IEnumerable<string> sourceLines)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(displayHeader);
    ArgumentNullException.ThrowIfNull(sourceLines);

    var lines = sourceLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
    if (lines.Count == 0)
    {
      return displayHeader;
    }

    lines[0] = System.Text.RegularExpressions.Regex.Replace(
      lines[0],
      @"^\s*\d+\s+\S+\s*",
      string.Empty,
      System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    string body = string.Join("\r\n  ", lines.Where(line => !string.IsNullOrWhiteSpace(line)));
    return string.IsNullOrWhiteSpace(body)
      ? displayHeader
      : $"{displayHeader}  {body}";
  }

  /// <summary>
  /// Выводит заголовок выполняемой команды программы контроля.
  /// </summary>
  public static Task PublishCommandExecutionAsync(
    IMessageOutputService outputService,
    string commandName,
    string? message = null,
    bool isBlockStart = true,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    var model = CommandMessageBuilder.BuildCommandExecutionMessage(commandName, message);
    return CommandMessagePublisher.PublishAsync(
      model, outputService, isBlockStart, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Выводит заголовок блока проверки, если включён вывод этапов проверки в протокол.
  /// </summary>
  public static Task PublishCheckBlockHeaderAsync(
    IMessageOutputService outputService,
    ControlCheckAlgorithm algorithm,
    bool inversion,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!ProtocolConfig.GetTestStepMessagesInProtocol())
    {
      return Task.CompletedTask;
    }

    var model = CommandMessageBuilder.BuildCheckBlockHeader(algorithm, inversion);
    return CommandMessagePublisher.PublishAsync(
      model, outputService, false, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Выводит заголовок проверки цепи, если включён вывод этапов проверки в протокол.
  /// </summary>
  public static Task PublishChainCheckBlockAsync(
    IMessageOutputService outputService,
    string chains,
    bool isBlockStart = true,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!ProtocolConfig.GetTestStepMessagesInProtocol())
    {
      return Task.CompletedTask;
    }

    var model = CommandMessageBuilder.BuildChainCheckBlock(chains);
    return CommandMessagePublisher.PublishAsync(
      model, outputService, isBlockStart, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Выводит заголовок проверки точек, если включён вывод этапов проверки в протокол.
  /// </summary>
  public static Task PublishPointsCheckHeaderAsync(
    IMessageOutputService outputService,
    PointModel firstPoint,
    PointModel secondPoint,
    CircuitFaultType circuitFaultType,
    bool isBlockStart = true,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!ProtocolConfig.GetTestStepMessagesInProtocol())
    {
      return Task.CompletedTask;
    }

    var model = CommandMessageBuilder.BuildPointsCheckHeader(firstPoint, secondPoint, circuitFaultType);
    return CommandMessagePublisher.PublishAsync(
      model, outputService, isBlockStart, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Выводит заголовок проверки разряда, если включён вывод этапов проверки в протокол.
  /// </summary>
  public static Task PublishDischargeCheckBlockAsync(
    IMessageOutputService outputService,
    int dischargeNumber,
    string dischargeView,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!ProtocolConfig.GetTestStepMessagesInProtocol())
    {
      return Task.CompletedTask;
    }

    var model = CommandMessageBuilder.BuildDischargeCheckBlock(dischargeNumber, dischargeView);
    return CommandMessagePublisher.PublishAsync(
      model, outputService, true, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Выводит сообщение об ошибке проверки разряда.
  /// </summary>
  public static Task PublishDischargeCheckErrorAsync(
    IMessageOutputService outputService,
    int dischargeNumber,
    string dischargeView,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    var model = CommandMessageBuilder.BuildDischargeCheckError(dischargeNumber, dischargeView);
    return CommandMessagePublisher.PublishAsync(
      model, outputService, true, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Выводит заголовок направления проверки диода.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="isDirectDirection">Признак проверки в прямом направлении.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishDiodeDirectionAsync(
    IMessageOutputService outputService,
    bool isDirectDirection,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    var model = CommandMessageBuilder.BuildDiodeDirectionMessage(isDirectDirection);
    return CommandMessagePublisher.PublishAsync(
      model, outputService, true, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Выводит заголовок подключения точек.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishPointsConnectionAsync(
    IMessageOutputService outputService,
    int indentLevel,
    bool isBlockStart = true,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    var model = CommandMessageBuilder.BuildPointsConnectionMessage(indentLevel);
    return CommandMessagePublisher.PublishAsync(
      model, outputService, isBlockStart, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Выводит заголовок команды, на которой сработала точка останова.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="commandNumber">Номер команды программы контроля.</param>
  /// <param name="mnemonic">Мнемоника команды.</param>
  /// <param name="commandBody">Тело команды.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishBreakpointHitAsync(
    IMessageOutputService outputService,
    string commandNumber,
    string mnemonic,
    string? commandBody,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    string commandName = $"{commandNumber} {mnemonic}".Trim();
    string displayedBody = string.IsNullOrWhiteSpace(commandBody) ? "<пусто>" : commandBody;
    var model = CommandMessageBuilder.BuildBreakpointHitMessage(commandName, displayedBody);

    return CommandMessagePublisher.PublishAsync(
      model,
      outputService,
      isBlockStart: true,
      callerName,
      callerFile,
      callerLine,
      skipStepModeCheck: true,
      skipPause: true);
  }

  /// <summary>
  /// Выводит сообщение о переходе к выбранной команде программы контроля.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="commandNumber">Номер целевой команды.</param>
  /// <param name="mnemonic">Мнемоника целевой команды.</param>
  /// <param name="commandBody">Тело целевой команды.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishCommandJumpAsync(
    IMessageOutputService outputService,
    string commandNumber,
    string mnemonic,
    string? commandBody,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    string commandName = $"{commandNumber} {mnemonic}".Trim();
    string displayedBody = string.IsNullOrWhiteSpace(commandBody) ? "<пусто>" : commandBody;
    var model = CommandMessageBuilder.BuildCommandJumpMessage(commandName, displayedBody);
    return CommandMessagePublisher.PublishAsync(
      model, outputService, true, callerName, callerFile, callerLine,
      skipStepModeCheck: true, skipPause: true);
  }

  /// <summary>
  /// Выводит заголовок начала выполнения программы контроля.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="objectName">Наименование объекта контроля.</param>
  /// <param name="objectCode">Обозначение объекта контроля.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishControlProgramStartAsync(
    IMessageOutputService outputService,
    string objectName,
    string objectCode,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    var model = CommandMessageBuilder.BuildControlProgramStartMessage(objectName, objectCode);
    return CommandMessagePublisher.PublishAsync(
      model,
      outputService,
      isBlockStart: true,
      callerName,
      callerFile,
      callerLine);
  }
}
