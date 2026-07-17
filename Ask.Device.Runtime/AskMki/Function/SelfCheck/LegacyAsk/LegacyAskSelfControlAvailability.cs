using Ask.Core.Services.Config.LegacyMki;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Проверяет доступность модулей самоконтроля старого тестера АСК по legacy-конфигурации.
/// </summary>
public static class LegacyAskSelfControlAvailability
{
  /// <summary>
  /// Проверяет, можно ли запускать указанный модуль самоконтроля.
  /// </summary>
  public static bool IsAvailable(LegacyMkiHardwareProfile profile, LegacyAskSelfControlModule module)
  {
    ArgumentNullException.ThrowIfNull(profile);

    var hardware = profile.HardwareConfig;

    return module switch
    {
      LegacyAskSelfControlModule.DigitalVoltmeter => hardware.DvV7 != 0 && hardware.DvV7 <= 8,
      LegacyAskSelfControlModule.Adc => hardware.DvAcp != 0,
      LegacyAskSelfControlModule.DeviceSwitching => IsAnyPintEnabled(hardware),
      LegacyAskSelfControlModule.Pints => IsAnyPintEnabled(hardware),
      LegacyAskSelfControlModule.Commutator => HasCommutatorRange(hardware),
      LegacyAskSelfControlModule.Ppu => hardware.TyPpu != 0,
      LegacyAskSelfControlModule.Pki => hardware.IsPki != 0,
      LegacyAskSelfControlModule.Timer => hardware.AcpTmr != 0 && IsPint4Enabled(hardware) && hardware.DivGatBk == 0,
      _ => false
    };
  }

  /// <summary>
  /// Проверяет, задан ли хотя бы один ПИНТ.
  /// </summary>
  private static bool IsAnyPintEnabled(LegacyMkiHardwareConfigSection hardware)
  {
    return hardware.GuiType.Any(value => value != 0);
  }

  /// <summary>
  /// Проверяет, задан ли ПИНТ4.
  /// </summary>
  private static bool IsPint4Enabled(LegacyMkiHardwareConfigSection hardware)
  {
    return hardware.GuiType.ElementAtOrDefault(1) != 0;
  }

  /// <summary>
  /// Проверяет, задан ли рабочий диапазон БК для коммутатора.
  /// </summary>
  private static bool HasCommutatorRange(LegacyMkiHardwareConfigSection hardware)
  {
    return hardware.SkBkBeg.Length > 0
      && hardware.SkBkEnd.Length > 0
      && hardware.SkBkEnd.Any(value => value > 0);
  }
}
