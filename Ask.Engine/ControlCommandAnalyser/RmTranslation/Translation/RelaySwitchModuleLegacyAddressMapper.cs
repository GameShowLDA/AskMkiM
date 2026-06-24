using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

public sealed class RelaySwitchModuleLegacyAddressMapper : ILegacyAddressMapper
{
  private const int LegacyModulePointCount = 100;
  private readonly IReadOnlyList<LegacyRelaySwitchModuleInfo> modules;

  public RelaySwitchModuleLegacyAddressMapper(IEnumerable<LegacyRelaySwitchModuleInfo> modules)
  {
    this.modules = modules
      .OrderBy(module => module.Number)
      .ToArray();
  }

  public LegacyAddressMapResult Map(MachineAddress address, TextSpan span)
  {
    var rackModules = modules
      .OrderBy(module => module.Number)
      .ToArray();

    if (rackModules.Length == 0)
    {
      return LegacyAddressMapResult.Failure(RmDiagnostic.Error(
        RmDiagnosticCode.LegacyCompatibilityAddress,
        "Режим совместимости АСК-МКИМ:" + Environment.NewLine +
        $"адрес {address} не может быть преобразован, потому что в конфигурации нет модулей коммутации.",
        span));
    }

    if (address.Point > LegacyModulePointCount)
    {
      return LegacyAddressMapResult.Failure(RmDiagnostic.Error(
        RmDiagnosticCode.LegacyCompatibilityAddress,
        "Режим совместимости АСК-МКИМ:" + Environment.NewLine +
        $"адрес {address} содержит точку {address.Point} в устаревшем модуле {address.Block}." + Environment.NewLine +
        Environment.NewLine +
        $"Максимально допустимый номер точки в устаревшем модуле: {LegacyModulePointCount}.",
        span));
    }

    var absolutePoint = ((address.Block - 1) * LegacyModulePointCount) + address.Point;
    var totalPointCount = rackModules.Sum(module => module.PointCount);
    if (absolutePoint > totalPointCount)
    {
      return LegacyAddressMapResult.Failure(RmDiagnostic.Error(
        RmDiagnosticCode.LegacyCompatibilityAddress,
        "Режим совместимости АСК-МКИМ:" + Environment.NewLine +
        $"адрес {address} соответствует сквозной точке {absolutePoint}, но такая точка отсутствует в конфигурации." + Environment.NewLine +
        Environment.NewLine +
        $"В конфигурации доступно только {totalPointCount} точек коммутации.",
        span));
    }

    var remainingPoint = absolutePoint;
    foreach (var module in rackModules)
    {
      if (remainingPoint <= module.PointCount)
        return LegacyAddressMapResult.Success(new MachineAddress(address.Rack, module.Number, remainingPoint));

      remainingPoint -= module.PointCount;
    }

    throw new InvalidOperationException("Не удалось преобразовать устаревший адрес после успешной проверки диапазона.");
  }
}
