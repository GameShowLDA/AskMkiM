namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

public sealed record ObjectAddressRange(string RawText, IReadOnlyList<ObjectAddress> Addresses)
{
  public int Count => Addresses.Count;
}
