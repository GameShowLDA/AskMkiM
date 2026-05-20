using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Validation;

public sealed class RmSemanticValidator
{
  public IReadOnlyList<RmDiagnostic> Validate(IReadOnlyList<AddressMapping> entries)
  {
    var diagnostics = new List<RmDiagnostic>();
    AddDuplicateDiagnostics(
      entries.GroupBy(entry => entry.ObjectAddress.Value, StringComparer.OrdinalIgnoreCase),
      RmDiagnosticCode.DuplicateObjectAddress,
      value => $"Повторяющийся адрес ОК: {value}.",
      diagnostics);

    AddDuplicateDiagnostics(
      entries.GroupBy(entry => entry.MachineAddress),
      RmDiagnosticCode.DuplicateMachineAddress,
      value => $"Повторяющийся машинный адрес: {value}.",
      diagnostics);

    AddDuplicateDiagnostics(
      entries
        .Where(entry => entry.Synonym is not null)
        .GroupBy(entry => entry.Synonym!.Value, StringComparer.OrdinalIgnoreCase),
      RmDiagnosticCode.DuplicateSynonym,
      value => $"Синоним уже используется: {value}.",
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

  private static void AddDuplicateDiagnostics<TKey>(
    IEnumerable<IGrouping<TKey, AddressMapping>> groups,
    RmDiagnosticCode code,
    Func<TKey, string> messageFactory,
    List<RmDiagnostic> diagnostics)
    where TKey : notnull
  {
    foreach (var group in groups.Where(group => group.Count() > 1))
    {
      foreach (var duplicate in group.Skip(1))
      {
        diagnostics.Add(RmDiagnostic.Error(code, messageFactory(group.Key), duplicate.SourceSpan));
      }
    }
  }
}
