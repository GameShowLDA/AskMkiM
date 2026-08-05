using Ask.Core.Shared.DTO.Protocol;
using Ask.Protocol.Messages.Models;

namespace Ask.Protocol.Messages.Extensions;

/// <summary>
/// Добавляет результаты выполнения алгоритмов в модель протокола.
/// </summary>
public static class ProtocolModelExtensions
{
  /// <summary>
  /// Добавляет ошибки и информационные сообщения алгоритма к результатам команды.
  /// </summary>
  /// <param name="protocolModel">Модель протокола выполнения программы контроля.</param>
  /// <param name="commandKey">Ключ команды в модели протокола.</param>
  /// <param name="result">Накопленные сообщения результата выполнения алгоритма.</param>
  public static void AddResult(
    this ProtocolModel protocolModel,
    string commandKey,
    AlgorithmExecutionResult result)
  {
    ArgumentNullException.ThrowIfNull(protocolModel);
    ArgumentException.ThrowIfNullOrWhiteSpace(commandKey);
    ArgumentNullException.ThrowIfNull(result);

    AddMessages(protocolModel.Errors, commandKey, result.Errors);
    AddMessages(protocolModel.Info, commandKey, result.Info);
  }

  private static void AddMessages(
    Dictionary<string, List<ShowMessageModel>> destination,
    string commandKey,
    List<ShowMessageModel> messages)
  {
    if (messages.Count == 0)
    {
      return;
    }

    if (destination.TryGetValue(commandKey, out var existing))
    {
      existing.AddRange(messages);
      return;
    }

    destination[commandKey] = new List<ShowMessageModel>(messages);
  }
}
