namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

public sealed record RmTranslationOptions(SynonymBindingMode SynonymBindingMode)
{
  public static RmTranslationOptions Default { get; } = new(SynonymBindingMode.ObjectThenSynonym);
}
