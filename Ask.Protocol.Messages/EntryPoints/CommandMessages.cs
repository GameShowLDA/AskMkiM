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
  /// Выводит заголовок выполняемой команды программы контроля.
  /// </summary>
  public static Task ShowCommandExecutionAsync(
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
  public static Task ShowCheckBlockHeaderAsync(
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
  public static Task ShowChainCheckBlockAsync(
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
  public static Task ShowPointsCheckHeaderAsync(
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
  public static Task ShowDischargeCheckBlockAsync(
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
  public static Task ShowDischargeCheckErrorAsync(
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
  public static Task ShowDiodeDirectionAsync(
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
  public static Task ShowPointsConnectionAsync(
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
  public static Task ShowBreakpointHitAsync(
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
}
