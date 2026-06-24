using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

public sealed record AddressMapping(
  ObjectAddress ObjectAddress,
  MachineAddress MachineAddress,
  ObjectAddress? Synonym,
  TextSpan SourceSpan)
{
  public MachineAddress SourceMachineAddress { get; init; } = MachineAddress;
}
