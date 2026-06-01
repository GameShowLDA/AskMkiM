using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

public sealed record Result<T>(T? Value, IReadOnlyList<RmDiagnostic> Diagnostics)
{
  public bool IsSuccess => Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);

  public static Result<T> Success(T value, IReadOnlyList<RmDiagnostic>? diagnostics = null)
    => new(value, diagnostics ?? Array.Empty<RmDiagnostic>());

  public static Result<T> Failure(IReadOnlyList<RmDiagnostic> diagnostics)
    => new(default, diagnostics);
}
