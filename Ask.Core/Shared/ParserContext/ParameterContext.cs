namespace Ask.Core.Shared.ParserContext
{
  public record ParameterContext(
    string CommandNumber,
    string Mnemonic,
    int LineNumber)
  {
    public static ParameterContext Create(string number, string mnemonic, int line)
        => new(number, mnemonic, line);
  }
}
