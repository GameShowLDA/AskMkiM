using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Lexer;

public sealed record RmLexerResult(
  IReadOnlyList<RmToken> Tokens,
  IReadOnlyList<RmDiagnostic> Diagnostics);
