using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Validation;

public sealed class RmSemanticValidator
{
  public IReadOnlyList<RmDiagnostic> Validate(IReadOnlyList<AddressMapping> entries)
  {
    var diagnostics = new List<RmDiagnostic>();
    AddStringDuplicateDiagnostics(
      entries.Select(entry => (entry.ObjectAddress.Value, entry.SourceSpan)),
      RmDiagnosticCode.DuplicateObjectAddress,
      value => $"Повторяющийся адрес ОК: {value}.",
      (first, last) => $"Повторяющиеся адреса ОК: {first}-{last}.",
      diagnostics);

    AddMachineAddressDuplicateDiagnostics(
      entries.Select(entry => (entry.MachineAddress, entry.SourceSpan)),
      RmDiagnosticCode.DuplicateMachineAddress,
      diagnostics);

    AddStringDuplicateDiagnostics(
      entries
        .Where(entry => entry.Synonym is not null)
        .Select(entry => (entry.Synonym!.Value, entry.SourceSpan)),
      RmDiagnosticCode.DuplicateSynonym,
      value => $"Синоним уже используется: {value}.",
      (first, last) => $"Синонимы уже используются: {first}-{last}.",
      diagnostics);

    var objectAddresses = entries
      .Select(entry => entry.ObjectAddress.Value)
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var entry in entries.Where(entry => entry.Synonym is not null))
    {
      if (objectAddresses.Contains(entry.Synonym!.Value))
      {
        diagnostics.Add(RmDiagnostic.Error(
          RmDiagnosticCode.SynonymObjectCollision,
          $"Синоним совпадает с адресом ОК: {entry.Synonym.Value}.",
          entry.SourceSpan));
      }
    }

    return diagnostics;
  }

  private static void AddStringDuplicateDiagnostics(
    IEnumerable<(string Value, TextSpan Span)> values,
    RmDiagnosticCode code,
    Func<string, string> singleMessageFactory,
    Func<string, string, string> rangeMessageFactory,
    List<RmDiagnostic> diagnostics)
  {
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var duplicates = new List<(string Value, TextSpan Span)>();
    foreach (var item in values)
    {
      if (!seen.Add(item.Value))
        duplicates.Add(item);
    }

    foreach (var group in GroupConsecutiveStrings(duplicates))
    {
      string message = group.Count == 1
        ? singleMessageFactory(group[0].Value)
        : rangeMessageFactory(group[0].Value, group[^1].Value);

      diagnostics.Add(RmDiagnostic.Error(code, message, group[0].Span));
    }
  }

  private static void AddMachineAddressDuplicateDiagnostics(
    IEnumerable<(MachineAddress Value, TextSpan Span)> values,
    RmDiagnosticCode code,
    List<RmDiagnostic> diagnostics)
  {
    var seen = new HashSet<MachineAddress>();
    var duplicates = new List<(MachineAddress Value, TextSpan Span)>();
    foreach (var item in values)
    {
      if (!seen.Add(item.Value))
        duplicates.Add(item);
    }

    foreach (var group in GroupConsecutiveMachineAddresses(duplicates))
    {
      string message = group.Count == 1
        ? $"Повторяющийся машинный адрес: {group[0].Value}."
        : $"Повторяющиеся машинные адреса: {group[0].Value}-{group[^1].Value}.";

      diagnostics.Add(RmDiagnostic.Error(code, message, group[0].Span));
    }
  }

  private static List<List<(string Value, TextSpan Span)>> GroupConsecutiveStrings(
    IReadOnlyList<(string Value, TextSpan Span)> values)
  {
    var result = new List<List<(string Value, TextSpan Span)>>();
    if (values.Count == 0)
      return result;

    var current = new List<(string Value, TextSpan Span)> { values[0] };
    int direction = 0;
    for (int i = 1; i < values.Count; i++)
    {
      var previous = current[^1];
      var next = values[i];
      string previousPrefix = string.Empty;
      string nextPrefix = string.Empty;
      int previousNumber = 0;
      int nextNumber = 0;
      bool consecutive = previous.Span == next.Span
        && TryGetTrailingNumber(previous.Value, out previousPrefix, out previousNumber)
        && TryGetTrailingNumber(next.Value, out nextPrefix, out nextNumber)
        && string.Equals(previousPrefix, nextPrefix, StringComparison.OrdinalIgnoreCase)
        && Math.Abs(nextNumber - previousNumber) == 1;
      int nextDirection = consecutive ? Math.Sign(nextNumber - previousNumber) : 0;

      if (consecutive && (direction == 0 || direction == nextDirection))
      {
        current.Add(next);
        direction = nextDirection;
        continue;
      }

      result.Add(current);
      current = new List<(string Value, TextSpan Span)> { next };
      direction = 0;
    }

    result.Add(current);
    return result;
  }

  private static List<List<(MachineAddress Value, TextSpan Span)>> GroupConsecutiveMachineAddresses(
    IReadOnlyList<(MachineAddress Value, TextSpan Span)> values)
  {
    var result = new List<List<(MachineAddress Value, TextSpan Span)>>();
    if (values.Count == 0)
      return result;

    var current = new List<(MachineAddress Value, TextSpan Span)> { values[0] };
    int direction = 0;
    for (int i = 1; i < values.Count; i++)
    {
      var previous = current[^1];
      var next = values[i];
      bool consecutive = previous.Span == next.Span
        && previous.Value.Rack == next.Value.Rack
        && previous.Value.Block == next.Value.Block
        && Math.Abs(next.Value.Point - previous.Value.Point) == 1;
      int nextDirection = consecutive
        ? Math.Sign(next.Value.Point - previous.Value.Point)
        : 0;

      if (consecutive && (direction == 0 || direction == nextDirection))
      {
        current.Add(next);
        direction = nextDirection;
        continue;
      }

      result.Add(current);
      current = new List<(MachineAddress Value, TextSpan Span)> { next };
      direction = 0;
    }

    result.Add(current);
    return result;
  }

  private static bool TryGetTrailingNumber(
    string value,
    out string prefix,
    out int number)
  {
    prefix = string.Empty;
    number = 0;

    int index = value.Length - 1;
    while (index >= 0 && char.IsDigit(value[index]))
      index--;

    if (index == value.Length - 1)
      return false;

    prefix = value[..(index + 1)];
    return int.TryParse(value[(index + 1)..], out number);
  }
}
