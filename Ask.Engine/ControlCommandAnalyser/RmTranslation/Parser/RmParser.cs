using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Lexer;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Parser;

public sealed class RmParser
{
  private readonly IReadOnlyList<RmToken> tokens;
  private readonly List<RmDiagnostic> diagnostics = new();
  private int position;

  private RmParser(IReadOnlyList<RmToken> tokens, IEnumerable<RmDiagnostic> lexerDiagnostics)
  {
    this.tokens = tokens;
    diagnostics.AddRange(lexerDiagnostics);
  }

  public static Result<TranslationDocumentAst> Parse(string text)
  {
    var lexerResult = RmLexer.Tokenize(text);
    var parser = new RmParser(lexerResult.Tokens, lexerResult.Diagnostics);
    return parser.ParseDocument();
  }

  private Result<TranslationDocumentAst> ParseDocument()
  {
    var mappings = new List<AddressMappingSyntax>();
    SkipWhitespace();

    if (Current.Kind == RmTokenKind.End)
    {
      diagnostics.Add(RmDiagnostic.Error(
        RmDiagnosticCode.EmptyInput,
        "Команда РМ должна содержать хотя бы одну запись.",
        Current.Span));
      return new Result<TranslationDocumentAst>(new TranslationDocumentAst(mappings), diagnostics);
    }

    while (Current.Kind != RmTokenKind.End)
    {
      var mapping = ParseMapping();
      if (mapping is not null)
        mappings.Add(mapping);

      SkipWhitespace();
    }

    return new Result<TranslationDocumentAst>(new TranslationDocumentAst(mappings), diagnostics);
  }

  private AddressMappingSyntax? ParseMapping()
  {
    SkipWhitespace();
    var start = Current.Span;
    var left = ParseExpressionUntilSeparator();

    if (left is null)
      return RecoverToNextEntry();

    AddressExpressionSyntax? middle = null;
    if (Current.Kind == RmTokenKind.DoubleEquals)
    {
      Advance();
      middle = ParseExpressionUntilSeparator();
      if (middle is null)
        return RecoverToNextEntry();
    }

    if (Current.Kind != RmTokenKind.Equals)
    {
      diagnostics.Add(RmDiagnostic.Error(
        RmDiagnosticCode.ExpectedEquals,
        "Ожидался знак '='.",
        Current.Span));
      return RecoverToNextEntry();
    }

    Advance();
    var machine = ParseMachineExpression();
    if (machine is null)
      return RecoverToNextEntry();

    var end = machine.Span.End;
    return new AddressMappingSyntax(
      left,
      middle,
      machine,
      TextSpan.FromBounds(start.Start, end, start.Line, start.Column));
  }

  private AddressExpressionSyntax? ParseExpressionUntilSeparator()
  {
    SkipWhitespace();
    if (Current.Kind is RmTokenKind.Equals or RmTokenKind.DoubleEquals or RmTokenKind.End)
    {
      diagnostics.Add(RmDiagnostic.Error(
        RmDiagnosticCode.EmptyExpression,
        "Ожидался адрес объекта контроля или синоним.",
        Current.Span));
      return null;
    }

    var startToken = Current;
    var parts = new List<string>();
    var end = startToken.Span.End;

    while (Current.Kind is not RmTokenKind.End and not RmTokenKind.Equals and not RmTokenKind.DoubleEquals)
    {
      if (Current.Kind == RmTokenKind.Whitespace)
      {
        var lookahead = PeekNonWhitespace();
        if (lookahead.Kind is RmTokenKind.Equals or RmTokenKind.DoubleEquals)
        {
          SkipWhitespace();
          break;
        }

        diagnostics.Add(RmDiagnostic.Error(
          RmDiagnosticCode.ExpectedEquals,
          "Ожидался знак '=' или '=='.",
          lookahead.Span));
        return null;
      }

      if (Current.Kind == RmTokenKind.Invalid)
      {
        Advance();
        continue;
      }

      parts.Add(Current.Text);
      end = Current.Span.End;
      Advance();
    }

    var value = string.Concat(parts).Trim();
    if (value.Length == 0)
    {
      diagnostics.Add(RmDiagnostic.Error(
        RmDiagnosticCode.EmptyExpression,
        "Ожидался адрес объекта контроля или синоним.",
        startToken.Span));
      return null;
    }

    return new AddressExpressionSyntax(value, TextSpan.FromBounds(startToken.Span.Start, end, startToken.Span.Line, startToken.Span.Column));
  }

  private AddressExpressionSyntax? ParseMachineExpression()
  {
    SkipWhitespace();
    if (Current.Kind is RmTokenKind.End or RmTokenKind.Equals or RmTokenKind.DoubleEquals)
    {
      diagnostics.Add(RmDiagnostic.Error(
        RmDiagnosticCode.ExpectedMachineAddress,
        "Ожидался машинный адрес.",
        Current.Span));
      return null;
    }

    var startToken = Current;
    var parts = new List<string>();
    var end = startToken.Span.End;

    while (Current.Kind != RmTokenKind.End && Current.Kind != RmTokenKind.Whitespace)
    {
      if (Current.Kind is RmTokenKind.Equals or RmTokenKind.DoubleEquals)
      {
        diagnostics.Add(RmDiagnostic.Error(
          RmDiagnosticCode.UnexpectedToken,
          "Неожиданный разделитель внутри машинного адреса.",
          Current.Span));
        return null;
      }

      if (Current.Kind == RmTokenKind.Invalid)
      {
        Advance();
        continue;
      }

      parts.Add(Current.Text);
      end = Current.Span.End;
      Advance();
    }

    var value = string.Concat(parts).Trim();
    if (value.Length == 0)
    {
      diagnostics.Add(RmDiagnostic.Error(
        RmDiagnosticCode.ExpectedMachineAddress,
        "Ожидался машинный адрес.",
        startToken.Span));
      return null;
    }

    return new AddressExpressionSyntax(value, TextSpan.FromBounds(startToken.Span.Start, end, startToken.Span.Line, startToken.Span.Column));
  }

  private AddressMappingSyntax? RecoverToNextEntry()
  {
    while (Current.Kind != RmTokenKind.End && Current.Kind != RmTokenKind.Whitespace)
      Advance();

    SkipWhitespace();
    return null;
  }

  private void SkipWhitespace()
  {
    while (Current.Kind == RmTokenKind.Whitespace)
      Advance();
  }

  private RmToken PeekNonWhitespace()
  {
    var index = position;
    while (index < tokens.Count && tokens[index].Kind == RmTokenKind.Whitespace)
      index++;

    return index < tokens.Count ? tokens[index] : tokens[^1];
  }

  private void Advance()
  {
    if (position < tokens.Count - 1)
      position++;
  }

  private RmToken Current => tokens[position];
}
