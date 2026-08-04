using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Protocol.Messages.Builders;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Предоставляет единые точки формирования сообщений команд программы контроля.
/// </summary>
public static class CommandMessages
{
  /// <summary>
  /// Формирует заголовок блока проверки по алгоритму контроля.
  /// </summary>
  /// <param name="algorithm">Алгоритм контроля.</param>
  /// <param name="inversion">Признак инверсии проверки.</param>
  /// <returns>Заголовок блока проверки.</returns>
  public static ShowMessageModel BuildCheckBlockHeader(ControlCheckAlgorithm algorithm, bool inversion)
    => CommandMessageBuilder.BuildCheckBlockHeader(algorithm, inversion);

  /// <summary>
  /// Формирует заголовок выполнения команды программы контроля.
  /// </summary>
  /// <param name="commandName">Имя команды программы контроля.</param>
  /// <param name="message">Отображаемый текст команды.</param>
  /// <returns>Заголовок выполнения команды.</returns>
  public static ShowMessageModel BuildCommandExecutionMessage(string commandName, string? message = null)
    => CommandMessageBuilder.BuildCommandExecutionMessage(commandName, message);

  /// <summary>
  /// Формирует заголовок блока проверки цепи.
  /// </summary>
  /// <param name="chains">Обозначение проверяемой цепи.</param>
  /// <returns>Заголовок блока проверки цепи.</returns>
  public static ShowMessageModel BuildChainCheckBlock(string chains)
    => CommandMessageBuilder.BuildChainCheckBlock(chains);

  /// <summary>
  /// Формирует заголовок проверки между двумя точками.
  /// </summary>
  /// <param name="firstPoint">Первая проверяемая точка.</param>
  /// <param name="secondPoint">Вторая проверяемая точка.</param>
  /// <param name="circuitFaultType">Тип проверяемой неисправности цепи.</param>
  /// <returns>Заголовок проверки между точками.</returns>
  public static ShowMessageModel BuildPointsCheckHeaderAsync(
    PointModel firstPoint,
    PointModel secondPoint,
    CircuitFaultType circuitFaultType)
    => CommandMessageBuilder.BuildPointsCheckHeader(firstPoint, secondPoint, circuitFaultType);

  /// <summary>
  /// Формирует заголовок блока проверки разряда.
  /// </summary>
  /// <param name="dischargeNumber">Номер проверяемого разряда.</param>
  /// <param name="dischargeView">Текстовое представление разряда.</param>
  /// <returns>Заголовок блока проверки разряда.</returns>
  public static ShowMessageModel BuildDischargeCheckBlock(int dischargeNumber, string dischargeView)
    => CommandMessageBuilder.BuildDischargeCheckBlock(dischargeNumber, dischargeView);

  /// <summary>
  /// Формирует сообщение об ошибке проверки разряда.
  /// </summary>
  /// <param name="dischargeNumber">Номер проверяемого разряда.</param>
  /// <param name="dischargeView">Текстовое представление разряда.</param>
  /// <returns>Сообщение об ошибке проверки разряда.</returns>
  public static ShowMessageModel BuildDischargeCheckError(int dischargeNumber, string dischargeView)
    => CommandMessageBuilder.BuildDischargeCheckError(dischargeNumber, dischargeView);
}
