using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Lexer;

public sealed class RmLexer
{
  private readonly string text;
  private readonly List<RmToken> tokens = new();
  private readonly List<RmDiagnostic> diagnostics = new();
  private int position;
  private int line = 1;
  private int column = 1;

  private RmLexer(string text)
  {
    this.text = text ?? string.Empty;
  }

  public static RmLexerResult Tokenize(string text)
  {
    var lexer = new RmLexer(text);
    lexer.Tokenize();
    return new RmLexerResult(lexer.tokens, lexer.diagnostics);
  }

  private void Tokenize()
  {
    while (!IsAtEnd)
    {
      var start = position;
      var startLine = line;
      var startColumn = column;
      var current = Current;

      if (char.IsWhiteSpace(current))
      {
        ReadWhile(char.IsWhiteSpace);
        Add(RmTokenKind.Whitespace, start, startLine, startColumn);
        continue;
      }

      if (current == '=')
      {
        Advance();
        if (!IsAtEnd && Current == '=')
        {
          Advance();
          Add(RmTokenKind.DoubleEquals, start, startLine, startColumn);
        }
        else
        {
          Add(RmTokenKind.Equals, start, startLine, startColumn);
        }

        continue;
      }

      if (IsForbidden(current))
      {
        Advance();
        var span = TextSpan.FromBounds(start, position, startLine, startColumn);
        tokens.Add(new RmToken(RmTokenKind.Invalid, text[start..position], span));
        diagnostics.Add(RmDiagnostic.Error(
          RmDiagnosticCode.UnexpectedCharacter,
          $"Недопустимый символ '{current}'.",
          span));
        continue;
      }

      ReadText();
      Add(RmTokenKind.Text, start, startLine, startColumn);
    }

    tokens.Add(new RmToken(RmTokenKind.End, string.Empty, new TextSpan(position, 0, line, column)));
  }

  private void ReadText()
  {
    while (!IsAtEnd
      && !char.IsWhiteSpace(Current)
      && Current != '='
      && !IsForbidden(Current))
    {
      Advance();
    }
  }

  private void ReadWhile(Func<char, bool> predicate)
  {
    while (!IsAtEnd && predicate(Current))
      Advance();
  }

  private void Add(RmTokenKind kind, int start, int startLine, int startColumn)
  {
    tokens.Add(new RmToken(kind, text[start..position], TextSpan.FromBounds(start, position, startLine, startColumn)));
  }

  private void Advance()
  {
    if (Current == '\n')
    {
      line++;
      column = 1;
    }
    else
    {
      column++;
    }

    position++;
  }

  private static bool IsForbidden(char ch)
  {
    return ch is '"' or '\'' or '$' or ';';
  }

  private bool IsAtEnd => position >= text.Length;

  private char Current => text[position];
}
