using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;

namespace Ask.Engine.ControlCommandAnalyser.Formatter.Base
{
  public abstract class CommandFormatter<TCommandModel> : ICommandFormatter
    where TCommandModel : BaseCommandModel
  {
    public bool CanFormat(BaseCommandModel model) => model is TCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      return model is TCommandModel commandModel
        ? Format(commandModel)
        : Enumerable.Empty<string>();
    }

    protected abstract IEnumerable<string> Format(TCommandModel model);

    protected static IEnumerable<string> FormatCommandStart(
      BaseCommandModel model,
      string? header = null,
      bool includeKey = true)
    {
      yield return header ?? FormatMnemonic(model);

      if (model is IHasUnparsedParameters unparsedModel
        && !string.IsNullOrWhiteSpace(unparsedModel.UnparsedParameters))
      {
        yield return $"\t{unparsedModel.UnparsedParameters}";
      }

      if (includeKey && model.AlgorithmKey.Count > 0)
      {
        yield return FormatKeys(model);
      }
    }

    protected static string FormatKeys(BaseCommandModel model)
    {
      return model.AlgorithmKey.Count > 0
        ? $"\tКлючи команды: {string.Join(", ", model.AlgorithmKey)}"
        : string.Empty;
    }

    protected static IEnumerable<string> FormatComments(BaseCommandModel model)
    {
      if (model.Comment.Count == 0)
      {
        yield break;
      }

      yield return "\tКомментарии:";

      for (int i = 0; i < model.Comment.Count; i++)
      {
        var line = model.Comment[i];
        if (string.IsNullOrWhiteSpace(line))
        {
          continue;
        }

        if (TryGetMultilineCommentBlock(model.Comment, i, out var blockLines, out var lastLineIndex))
        {
          foreach (var blockLine in FormatMultilineCommentBlock(blockLines))
          {
            yield return $"\t\t{blockLine}";
          }

          i = lastLineIndex;
          continue;
        }

        yield return $"\t\t{line.Trim()}";
      }
    }

    protected static IEnumerable<string> FormatEnd()
    {
      yield return string.Empty;
    }

    protected static IEnumerable<string> FormatSchemeWithRmCheckDisconnectedPoints(
      IHasScheme model,
      string title,
      string rmNotSetMessage = "\t\tРњРѕРґРµР»СЊ Р Рњ РЅРµ Р·Р°РґР°РЅР°!")
    {
      yield return title;

      if (!HasRmModel())
      {
        yield return rmNotSetMessage;
        yield break;
      }

      foreach (var line in SchemeFormatter.FormatDisconnectedPoints(model.Scheme))
      {
        yield return line;
      }
    }

    protected static IEnumerable<string> FormatSchemeWithRmCheckConnectedPoints(
      IHasScheme model,
      string title,
      string rmNotSetMessage = "\t\tРњРѕРґРµР»СЊ Р Рњ РЅРµ Р·Р°РґР°РЅР°!")
    {
      yield return title;

      if (!HasRmModel())
      {
        yield return rmNotSetMessage;
        yield break;
      }

      foreach (var line in SchemeFormatter.FormatSchemeConnectedPoints(model))
      {
        yield return line;
      }
    }

    protected static bool HasRmModel()
    {
      return CommandsModel.GetRMModel() != null;
    }

    protected static IEnumerable<string> FormatBusPointGroups(
      Dictionary<SwitchingBus, List<PointModel>> busPoints,
      string title)
    {
      if (busPoints.Count == 0)
      {
        yield return $"{title} РЅРµ Р·Р°РґР°РЅС‹!";
        yield break;
      }

      foreach (var bus in busPoints)
      {
        yield return $"{title}: {bus.Key}";

        foreach (var point in bus.Value)
        {
          yield return $"\t\t{point.Mnemonic} = {point}";
        }
      }
    }

    private static bool TryGetMultilineCommentBlock(
      IReadOnlyList<string> comments,
      int startIndex,
      out List<string> blockLines,
      out int lastLineIndex)
    {
      blockLines = new List<string>();
      lastLineIndex = startIndex;

      var firstLine = comments[startIndex];
      if (!TryGetBlockDescriptor(firstLine, out var descriptor))
      {
        return false;
      }

      blockLines.Add(firstLine);

      if (ContainsClosingToken(firstLine, descriptor, skipOpeningToken: true))
      {
        return false;
      }

      for (int i = startIndex + 1; i < comments.Count; i++)
      {
        blockLines.Add(comments[i]);
        lastLineIndex = i;

        if (ContainsClosingToken(comments[i], descriptor, skipOpeningToken: false))
        {
          return true;
        }
      }

      return blockLines.Count > 1;
    }

    private static IEnumerable<string> FormatMultilineCommentBlock(IReadOnlyList<string> blockLines)
    {
      if (blockLines.Count == 0)
      {
        yield break;
      }

      var firstLine = blockLines[0].TrimStart().TrimEnd();
      yield return firstLine;

      if (!TryGetBlockDescriptor(blockLines[0], out var descriptor))
      {
        for (int i = 1; i < blockLines.Count; i++)
        {
          if (!string.IsNullOrWhiteSpace(blockLines[i]))
          {
            yield return blockLines[i].Trim();
          }
        }

        yield break;
      }

      var continuationIndent = GetContinuationIndent(firstLine, descriptor);
      var commonIndent = GetCommonIndent(blockLines, descriptor);

      for (int i = 1; i < blockLines.Count; i++)
      {
        var line = blockLines[i];
        if (string.IsNullOrWhiteSpace(line))
        {
          continue;
        }

        var trimmedStart = line.TrimStart().TrimEnd();
        var leadingIndent = GetLeadingWhitespaceCount(line);
        var extraIndent = Math.Max(0, leadingIndent - commonIndent);

        yield return new string(' ', continuationIndent + extraIndent) + trimmedStart;
      }
    }

    private static bool TryGetBlockDescriptor(string line, out (string Open, string Close) descriptor)
    {
      var trimmedStart = line.TrimStart();

      if (trimmedStart.StartsWith('{'))
      {
        descriptor = ("{", "}");
        return true;
      }

      if (trimmedStart.StartsWith("/*"))
      {
        descriptor = ("/*", "*/");
        return true;
      }

      descriptor = default;
      return false;
    }

    private static bool ContainsClosingToken(string line, (string Open, string Close) descriptor, bool skipOpeningToken)
    {
      var trimmedStart = line.TrimStart();
      var searchStart = skipOpeningToken && trimmedStart.StartsWith(descriptor.Open)
        ? descriptor.Open.Length
        : 0;

      return trimmedStart.IndexOf(descriptor.Close, searchStart, StringComparison.Ordinal) >= 0;
    }

    private static int GetContinuationIndent(string firstLine, (string Open, string Close) descriptor)
    {
      var openIndex = firstLine.IndexOf(descriptor.Open, StringComparison.Ordinal);
      if (openIndex < 0)
      {
        return 0;
      }

      for (int i = openIndex + descriptor.Open.Length; i < firstLine.Length; i++)
      {
        if (!char.IsWhiteSpace(firstLine[i]))
        {
          return i;
        }
      }

      return 0;
    }

    private static int GetCommonIndent(IReadOnlyList<string> blockLines, (string Open, string Close) descriptor)
    {
      int? commonIndent = null;

      for (int i = 1; i < blockLines.Count; i++)
      {
        var line = blockLines[i];
        if (string.IsNullOrWhiteSpace(line))
        {
          continue;
        }

        var trimmed = line.Trim();
        if (trimmed == descriptor.Close)
        {
          continue;
        }

        var leadingIndent = GetLeadingWhitespaceCount(line);
        commonIndent = commonIndent.HasValue
          ? Math.Min(commonIndent.Value, leadingIndent)
          : leadingIndent;
      }

      return commonIndent ?? 0;
    }

    private static int GetLeadingWhitespaceCount(string line)
    {
      int count = 0;
      while (count < line.Length && char.IsWhiteSpace(line[count]))
      {
        count++;
      }

      return count;
    }

    private static string FormatMnemonic(BaseCommandModel model)
    {
      return $"{model.CommandNumber} {model.Mnemonic}";
    }
  }
}
