using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Описывает один пункт выбора самоконтроля старого тестера АСК.
/// </summary>
public sealed class LegacyAskSelfControlTarget
{
  /// <summary>
  /// Создаёт пункт самоконтроля старого тестера АСК.
  /// </summary>
  public LegacyAskSelfControlTarget(
    int numberChassis,
    string chassisName,
    LegacyAskSelfControlModule module)
  {
    NumberChassis = numberChassis;
    ChassisName = chassisName;
    Module = module;
  }

  /// <summary>
  /// Возвращает номер стойки, для которой запускается самоконтроль.
  /// </summary>
  public int NumberChassis { get; }

  /// <summary>
  /// Возвращает название стойки из конфигурации устройств.
  /// </summary>
  public string ChassisName { get; }

  /// <summary>
  /// Возвращает выбранный модуль самоконтроля.
  /// </summary>
  public LegacyAskSelfControlModule Module { get; }

  /// <summary>
  /// Создаёт пункты самоконтроля для указанной стойки АСК.
  /// </summary>
  public static IReadOnlyList<LegacyAskSelfControlTarget> CreateForChassis(IChassisManager chassis)
  {
    return Enum.GetValues<LegacyAskSelfControlModule>()
      .Select(module => new LegacyAskSelfControlTarget(chassis.Number, chassis.Name ?? "Тестер АСК", module))
      .ToList();
  }

  /// <inheritdoc />
  public override string ToString() => $"{ChassisName} {NumberChassis}: {LegacyAskSelfControlModuleMetadata.GetDisplayName(Module)}";
}
