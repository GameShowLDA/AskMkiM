using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

public sealed class RelaySwitchModuleAddressValidator : ILegacyAddressMapper
{
  private readonly IReadOnlyList<LegacyRelaySwitchModuleInfo> modules;

  public RelaySwitchModuleAddressValidator(IEnumerable<LegacyRelaySwitchModuleInfo> modules)
  {
    this.modules = modules.ToArray();
  }

  public LegacyAddressMapResult Map(MachineAddress address, TextSpan span)
  {
    var module = modules.FirstOrDefault(module => module.Number == address.Block);

    if (module is null)
    {
      return LegacyAddressMapResult.Failure(RmDiagnostic.Error(
        RmDiagnosticCode.MachineAddressNotConfigured,
        $"Машинный адрес {address}: модуль {address.Block} отсутствует в конфигурации.",
        span));
    }

    if (address.Point > module.PointCount)
    {
      return LegacyAddressMapResult.Failure(RmDiagnostic.Error(
        RmDiagnosticCode.MachineAddressNotConfigured,
        $"Машинный адрес {address}: точка {address.Point} отсутствует на модуле {module.Number}." + Environment.NewLine +
        Environment.NewLine +
        $"Максимально допустимый номер точки: {module.PointCount}.",
        span));
    }

    return LegacyAddressMapResult.Success(address);
  }
}
