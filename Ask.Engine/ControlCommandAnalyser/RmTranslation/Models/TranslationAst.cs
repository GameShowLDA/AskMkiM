using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

public sealed record TranslationDocumentAst(IReadOnlyList<AddressMappingSyntax> Mappings);

public sealed record AddressMappingSyntax(
  AddressExpressionSyntax Left,
  AddressExpressionSyntax? Middle,
  AddressExpressionSyntax Machine,
  TextSpan Span);

public sealed record AddressExpressionSyntax(string Text, TextSpan Span);
