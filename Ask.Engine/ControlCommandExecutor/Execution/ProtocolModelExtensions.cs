using System;
using System.Linq;
using System.Text.RegularExpressions;
using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  internal static class ProtocolModelExtensions
  {
    public static void AddErrors(this ProtocolModel protocolModel, string commandKey, List<ShowMessageModel> errors)
    {
      AddMessages(protocolModel, commandKey, errors, ProtocolModel.ProtocolMessageKind.Error);
    }

    public static void AddInfo(this ProtocolModel protocolModel, string commandKey, List<ShowMessageModel> info)
    {
      AddMessages(protocolModel, commandKey, info, ProtocolModel.ProtocolMessageKind.Information);
    }

    private static void AddMessages(
      ProtocolModel protocolModel,
      string commandKey,
      List<ShowMessageModel> messages,
      ProtocolModel.ProtocolMessageKind kind)
    {
      if (messages == null || messages.Count == 0)
      {
        return;
      }

      if (!protocolModel.Messages.TryGetValue(commandKey, out var existing))
      {
        existing = new List<(ShowMessageModel Message, ProtocolModel.ProtocolMessageKind Kind)>();
        protocolModel.Messages[commandKey] = existing;
      }

      foreach (var message in messages)
      {
        if (kind == ProtocolModel.ProtocolMessageKind.Information &&
            HasMatchingError(existing, message))
        {
          continue;
        }

        if (kind == ProtocolModel.ProtocolMessageKind.Error)
        {
          existing.RemoveAll(entry =>
            entry.Kind == ProtocolModel.ProtocolMessageKind.Information &&
            AreSameProtocolMessage(entry.Message, message));
        }

        existing.Add((message, kind));
      }
    }

    private static bool HasMatchingError(
      List<(ShowMessageModel Message, ProtocolModel.ProtocolMessageKind Kind)> existing,
      ShowMessageModel message)
    {
      return existing.Any(entry =>
        entry.Kind == ProtocolModel.ProtocolMessageKind.Error &&
        AreSameProtocolMessage(entry.Message, message));
    }

    private static bool AreSameProtocolMessage(ShowMessageModel left, ShowMessageModel right)
    {
      return string.Equals(NormalizeProtocolText(left?.Header), NormalizeProtocolText(right?.Header), StringComparison.Ordinal) &&
             string.Equals(NormalizeProtocolText(left?.Message), NormalizeProtocolText(right?.Message), StringComparison.Ordinal);
    }

    private static string NormalizeProtocolText(string? value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return string.Empty;
      }

      var normalized = Regex.Replace(value.Trim(), @"\s+\(", "(");
      normalized = Regex.Replace(normalized, @"\s+", " ");
      return normalized;
    }
  }
}
