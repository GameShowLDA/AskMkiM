namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

public sealed record RmTranslationOptions(
  SynonymBindingMode SynonymBindingMode,
  ILegacyAddressMapper? LegacyAddressMapper = null)
{
  public static RmTranslationOptions Default { get; } = new(SynonymBindingMode.ObjectThenSynonym);
}
