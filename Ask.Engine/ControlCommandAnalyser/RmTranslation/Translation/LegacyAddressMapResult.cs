using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

public sealed record LegacyAddressMapResult(
  MachineAddress? Address,
  IReadOnlyList<RmDiagnostic> Diagnostics)
{
  public bool IsSuccess => Address.HasValue && !Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

  public static LegacyAddressMapResult Success(MachineAddress address)
    => new(address, Array.Empty<RmDiagnostic>());

  public static LegacyAddressMapResult Failure(RmDiagnostic diagnostic)
    => new(null, new[] { diagnostic });
}
