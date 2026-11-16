using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Engine.Equipment;

/// <summary>
/// Определяет доступные пары шин по установленным модулям коммутации реле.
/// </summary>
internal static class RelayModuleBusPairResolver
{
  /// <summary>
  /// Объединяет пары шин модулей, относящихся к указанному тестеру.
  /// </summary>
  /// <param name="modules">Настроенные модули коммутации реле.</param>
  /// <param name="chassisNumber">Номер тестера.</param>
  /// <returns>Набор пар шин, представленных установленными модулями.</returns>
  public static SupportedBusPairs Resolve(
    IEnumerable<IRelaySwitchModule> modules,
    int chassisNumber)
  {
    ArgumentNullException.ThrowIfNull(modules);

    return modules
      .Where(module => module.NumberChassis == chassisNumber)
      .Aggregate(
        SupportedBusPairs.None,
        (pairs, module) => pairs | SupportedBusPairsExtensions.FromBus(module.BusType));
  }
}
