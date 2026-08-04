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
}
