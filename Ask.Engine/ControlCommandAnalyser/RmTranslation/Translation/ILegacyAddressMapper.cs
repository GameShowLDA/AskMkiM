using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

public interface ILegacyAddressMapper
{
  LegacyAddressMapResult Map(MachineAddress address, TextSpan span);
}
