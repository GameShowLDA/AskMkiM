namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

public sealed record ObjectAddress(string Value)
{
  public override string ToString() => Value;
}
