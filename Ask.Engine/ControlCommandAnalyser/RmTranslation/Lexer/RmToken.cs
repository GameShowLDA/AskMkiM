using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Lexer;

public sealed record RmToken(RmTokenKind Kind, string Text, TextSpan Span);
