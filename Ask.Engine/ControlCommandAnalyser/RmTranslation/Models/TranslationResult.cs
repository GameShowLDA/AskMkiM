using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

public sealed record TranslationResult(
  IReadOnlyList<AddressMapping> Entries,
  IReadOnlyList<RmDiagnostic> Diagnostics)
{
  public bool IsSuccess => Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);

  public AddressTranslationIndex CreateIndex() => new(Entries);
}
