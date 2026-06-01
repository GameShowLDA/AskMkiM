namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;

public readonly record struct TextSpan(int Start, int Length, int Line, int Column)
{
  public int End => Start + Length;

  public static TextSpan FromBounds(int start, int end, int line, int column)
  {
    return new TextSpan(start, Math.Max(0, end - start), line, column);
  }
}
