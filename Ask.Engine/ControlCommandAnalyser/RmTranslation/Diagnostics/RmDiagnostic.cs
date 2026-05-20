namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;

public sealed record RmDiagnostic(
  DiagnosticSeverity Severity,
  RmDiagnosticCode Code,
  string Message,
  TextSpan Span)
{
  public static RmDiagnostic Error(RmDiagnosticCode code, string message, TextSpan span)
    => new(DiagnosticSeverity.Error, code, message, span);
}
