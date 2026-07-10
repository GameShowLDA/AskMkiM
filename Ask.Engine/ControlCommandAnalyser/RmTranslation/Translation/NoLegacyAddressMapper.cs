using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

public sealed class NoLegacyAddressMapper : ILegacyAddressMapper
{
  public static NoLegacyAddressMapper Instance { get; } = new();

  private NoLegacyAddressMapper()
  {
  }

  public LegacyAddressMapResult Map(MachineAddress address, TextSpan span)
    => LegacyAddressMapResult.Success(address);
}
