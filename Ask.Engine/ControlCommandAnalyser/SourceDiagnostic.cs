namespace Ask.Engine.ControlCommandAnalyser;

/// <summary>Diagnostic span in the unmodified editor snapshot (UTF-16 offsets).</summary>
public sealed record SourceDiagnostic(
  int Offset, int Length, int LineNumber, bool IsWarning, string Description, string? Code);
