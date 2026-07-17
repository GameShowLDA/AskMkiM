using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Device.Runtime.Device.ASKMKI;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Самоконтроль цифрового вольтметра старого тестера АСК.
/// </summary>

internal static class LegacyAskSelfTestFormat
{
  /// <summary>
  /// Возвращает слово напряжения в милливольтах.
  /// </summary>
  public static ushort ToMillivoltsWord(double volts)
  {
    return (ushort)Math.Clamp((int)Math.Round(volts * 1000.0), 0, ushort.MaxValue);
  }

  /// <summary>
  /// Возвращает слово тока в миллиамперах.
  /// </summary>
  public static ushort ToMilliampsWord(double amps)
  {
    return (ushort)Math.Clamp((int)Math.Round(amps * 1000.0), 0, ushort.MaxValue);
  }

  /// <summary>
  /// Возвращает регистр управления ПИНТом.
  /// </summary>
  public static LegacyAskRegister GetPintRegister(int pint)
  {
    return pint == 3 ? LegacyAskRegister.Gui3 : LegacyAskRegister.Gui4;
  }

  /// <summary>
  /// Устанавливает напряжение, ток и шины ПИНТа через те же подрегистры, что использовала старая MKI.
  /// </summary>
  public static async Task SetPintOutputAsync(
    LegacyAskSelfControlContext context,
    IAskMkiController controller,
    int pint,
    double volts,
    double amps,
    ushort positiveBus,
    ushort negativeBus)
  {
    await context.Devices.GetRequiredPint(pint).SetOutputAsync(controller, volts, amps, positiveBus, negativeBus, context.CancellationToken);
  }

  /// <summary>
  /// Подключает плюсовой и минусовой выходы ПИНТа к шинам старого контроллера.
  /// </summary>
  public static async Task SetPintBusesAsync(
    LegacyAskSelfControlContext context,
    IAskMkiController controller,
    int pint,
    ushort positiveBus,
    ushort negativeBus)
  {
    await context.Devices.GetRequiredPint(pint).SetBusesAsync(controller, positiveBus, negativeBus, context.CancellationToken);
  }

  /// <summary>
  /// Сбрасывает ПИНТ в малый режим и отключает его от шин.
  /// </summary>
  public static async Task ResetPintAsync(LegacyAskSelfControlContext context, IAskMkiController controller, int pint)
  {
    await context.Devices.GetRequiredPint(pint).ResetAsync(controller, context.CancellationToken);
  }

  /// <summary>
  /// Устанавливает режим АЦП и подключение его входов к шинам.
  /// </summary>
  public static async Task SetAcpModeAsync(
    LegacyAskSelfControlContext context,
    IAskMkiController controller,
    ushort mode,
    ushort positiveBus,
    ushort negativeBus)
  {
    await context.Devices.Acp.SetModeAsync(controller, mode, positiveBus, negativeBus, context.CancellationToken);
  }

  /// <summary>
  /// Выполняет одно измерение АЦП после установки режима и шин.
  /// </summary>
  public static async Task<ushort> ReadAcpAsync(
    LegacyAskSelfControlContext context,
    IAskMkiController controller,
    ushort mode,
    ushort positiveBus,
    ushort negativeBus)
  {
    return await context.Devices.Acp.ReadAsync(controller, mode, positiveBus, negativeBus, context.CancellationToken);
  }

  /// <summary>
  /// Читает напряжение АЦП и переводит сырое слово контроллера в вольты.
  /// </summary>
  public static async Task<double> ReadAcpVoltageAsync(
    LegacyAskSelfControlContext context,
    IAskMkiController controller,
    ushort mode,
    ushort positiveBus,
    ushort negativeBus,
    double expected)
  {
    ushort raw = await ReadAcpAsync(context, controller, mode, positiveBus, negativeBus);
    if (ExecutionConfig.GetIsIdleModeEnabled())
    {
      LogInformation($"АСК idle: эмуляция АЦП U={expected:0.####}.", isDeviceLog: true);
      return expected;
    }

    if ((raw & 0x4000) != 0)
    {
      return double.PositiveInfinity;
    }

    double range = GetAcpVoltageRange(mode);
    double value = (raw & 0x03FF) * range * 1.1 / 1000.0;
    return (raw & 0x2000) != 0 ? value : -value;
  }

  /// <summary>
  /// Читает сопротивление АЦП и переводит сырое слово контроллера в омы.
  /// </summary>
  public static async Task<double> ReadAcpResistanceAsync(
    LegacyAskSelfControlContext context,
    IAskMkiController controller,
    ushort mode,
    ushort positiveBus,
    ushort negativeBus,
    double expected)
  {
    ushort raw = await ReadAcpAsync(context, controller, mode, positiveBus, negativeBus);
    if (ExecutionConfig.GetIsIdleModeEnabled())
    {
      LogInformation($"АСК idle: эмуляция АЦП R={expected:0.####}.", isDeviceLog: true);
      return expected;
    }

    if ((raw & 0x5000) != 0)
    {
      return double.PositiveInfinity;
    }

    double range = GetAcpResistanceRange(mode);
    return (raw & 0x03FF) * range * 1.1 / 1000.0;
  }

  /// <summary>
  /// Выполняет аппаратную задержку только в боевом режиме.
  /// </summary>
  public static async Task DelayIfHardwareAsync(LegacyAskSelfControlContext context, int milliseconds, string reason)
  {
    if (milliseconds <= 0 || ExecutionConfig.GetIsIdleModeEnabled())
    {
      return;
    }

    LogInformation($"АСК задержка {reason}: {milliseconds} мс.", isDeviceLog: true);
    await Task.Delay(milliseconds, context.CancellationToken);
  }

  /// <summary>
  /// Кодирует напряжение ПИНТа в дискретный код старой MKI с учетом шага из конфигурации.
  /// </summary>
  public static ushort ToPintVoltageWord(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile, int pint, double volts)
  {
    double step = PositiveOrDefault(profile.HardwareConfig.GuiVoltStep.ElementAtOrDefault(pint - 3), 0.1);
    int code = (int)Math.Round(Math.Max(0.0, volts) / step);
    return ToPintCode(profile, pint, code <= 0 && volts > 0 ? 1 : code);
  }

  /// <summary>
  /// Кодирует ток ПИНТа в дискретный код старой MKI с учетом шага из конфигурации.
  /// </summary>
  public static ushort ToPintCurrentWord(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile, int pint, double amps)
  {
    double fallbackStep = pint == 3 ? 0.1 : 0.001;
    double step = PositiveOrDefault(profile.HardwareConfig.GuiAmperStep.ElementAtOrDefault(pint - 3), fallbackStep);
    int code = (int)Math.Round(Math.Max(0.0, amps) / step);
    return ToPintCode(profile, pint, code <= 0 && amps > 0 ? 1 : code);
  }

  /// <summary>
  /// Кодирует дискрет ПИНТа в формат 2-10 или двоичный формат выбранного типа ПИНТа.
  /// </summary>
  private static ushort ToPintCode(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile, int pint, int code)
  {
    int safeCode = Math.Clamp(code, 0, 0x0FFF);
    byte type = profile.HardwareConfig.GuiType.ElementAtOrDefault(pint - 3);
    return type == 1 ? ToBcdWord(safeCode) : (ushort)safeCode;
  }

  /// <summary>
  /// Кодирует число в 2-10 код старых ПУИ.
  /// </summary>
  private static ushort ToBcdWord(int value)
  {
    int safeValue = Math.Clamp(value, 0, 999);
    return (ushort)(((safeValue / 100) << 8) | (((safeValue / 10) % 10) << 4) | (safeValue % 10));
  }

  /// <summary>
  /// Возвращает слово шин ПИНТа без битов подадреса.
  /// </summary>
  private static ushort ToPintBusWord(ushort bus)
  {
    return (ushort)(bus & 0x00FF);
  }

  /// <summary>
  /// Возвращает слово подключения плюса и минуса АЦП к шинам MKI.
  /// </summary>
  private static ushort ToAcpGateWord(ushort positiveBus, ushort negativeBus)
  {
    ushort word = 0;
    if ((positiveBus & LegacyAskBus.A1) != 0) word |= 0x0001;
    if ((positiveBus & LegacyAskBus.B1) != 0) word |= 0x0002;
    if ((positiveBus & LegacyAskBus.A2) != 0) word |= 0x0004;
    if ((positiveBus & LegacyAskBus.B2) != 0) word |= 0x0008;
    if ((negativeBus & LegacyAskBus.A1) != 0) word |= 0x0010;
    if ((negativeBus & LegacyAskBus.B1) != 0) word |= 0x0020;
    if ((negativeBus & LegacyAskBus.A2) != 0) word |= 0x0040;
    if ((negativeBus & LegacyAskBus.B2) != 0) word |= 0x0080;
    return word;
  }

  /// <summary>
  /// Возвращает максимальное напряжение ППУ по legacy-конфигурации.
  /// </summary>
  public static int GetPpuMaximumVoltage(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile)
  {
    return profile.HardwareConfig.TyPpu == 0 ? 0 : 625;
  }

  /// <summary>
  /// Возвращает максимальное напряжение короткого теста ППУ с учетом напряжения сети.
  /// </summary>
  public static int GetPpuConfiguredMaximumVoltage(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile)
  {
    int maximum = GetPpuMaximumVoltage(profile);
    if (maximum <= 0)
    {
      return 0;
    }

    int mains = profile.HardwareAux.U220 == 0 ? 220 : profile.HardwareAux.U220;
    return (int)Math.Round(maximum * (mains - 5.0) / 220.0);
  }

  /// <summary>
  /// Форматирует напряжение как в старом протоколе.
  /// </summary>
  public static string Voltage(double volts)
  {
    if (Math.Abs(volts) < 0.0000001)
    {
      return "0.0000В";
    }

    if (Math.Abs(volts) < 1.0)
    {
      return $"{(volts * 1000.0).ToString("0.#", CultureInfo.InvariantCulture)}мВ";
    }

    return $"{volts.ToString("0.####", CultureInfo.InvariantCulture)}В";
  }

  /// <summary>
  /// Форматирует ток как в старом протоколе.
  /// </summary>
  public static string Current(double amps)
  {
    if (Math.Abs(amps) < 1.0)
    {
      return $"{(amps * 1000.0).ToString("0.###", CultureInfo.InvariantCulture)}мА";
    }

    return $"{amps.ToString("0.###", CultureInfo.InvariantCulture)}А";
  }

  /// <summary>
  /// Форматирует сопротивление как в старом протоколе.
  /// </summary>
  public static string Resistance(double ohms)
  {
    if (ohms >= 1_000_000.0)
    {
      return $"{(ohms / 1_000_000.0).ToString("0.###", CultureInfo.InvariantCulture)}МОм";
    }

    if (ohms >= 1000.0)
    {
      return $"{(ohms / 1000.0).ToString("0.###", CultureInfo.InvariantCulture)}кОм";
    }

    return $"{ohms.ToString("0.##", CultureInfo.InvariantCulture)} Ом";
  }

  /// <summary>
  /// Возвращает положительное значение или значение по умолчанию.
  /// </summary>
  public static double PositiveOrDefault(double value, double fallback)
  {
    return value > 0 ? value : fallback;
  }

  /// <summary>
  /// Генерирует декадные точки старых тестов ПИНТов.
  /// </summary>
  public static IEnumerable<double> DecadeValues(double step, double max)
  {
    for (double decade = 1.0; step * decade <= max * 1.000001; decade *= 10.0)
    {
      for (int digit = 1; digit <= 9; digit++)
      {
        double value = step * decade * digit;
        if (value > max * 1.000001)
        {
          yield break;
        }

        yield return value;
      }
    }
  }

  /// <summary>
  /// Возвращает список имеющихся ПИНТов.
  /// </summary>
  public static IEnumerable<int> GetPresentPints(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile)
  {
    if (profile.HardwareConfig.GuiType.ElementAtOrDefault(0) != 0)
    {
      yield return 3;
    }

    if (profile.HardwareConfig.GuiType.ElementAtOrDefault(1) != 0)
    {
      yield return 4;
    }
  }

  /// <summary>
  /// Возвращает пары шин для проверки коммутации устройств.
  /// </summary>
  public static IEnumerable<LegacyAskBusPair> DeviceSwitchBuses()
  {
    yield return new LegacyAskBusPair(LegacyAskBus.A1, LegacyAskBus.B1, "A1", "B1");
    yield return new LegacyAskBusPair(LegacyAskBus.A2, LegacyAskBus.B2, "A2", "B2");
    yield return new LegacyAskBusPair(LegacyAskBus.A1, LegacyAskBus.B2, "A1", "B2");
    yield return new LegacyAskBusPair(LegacyAskBus.A2, LegacyAskBus.B1, "A2", "B1");
  }

  /// <summary>
  /// Возвращает диапазоны стоек и БК для проверки коммутатора.
  /// </summary>
  public static IEnumerable<LegacyAskSwitchRange> GetSwitchRanges(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile)
  {
    string[] names = ["ТКИ", "СК-2400 2", "СК-2400 3", "СК-2400 4", "СК-2400 5", "СК-2400 6", "СК-2400 7", "СК-2400 8"];
    for (int index = 0; index < names.Length; index++)
    {
      bool isPresent = index == 0 || profile.HardwareConfig.SkIs.ElementAtOrDefault(index) != 0;
      if (!isPresent)
      {
        continue;
      }

      int first = Math.Max(1, (int)profile.HardwareConfig.SkBkBeg.ElementAtOrDefault(index));
      int last = Math.Max(first, (int)profile.HardwareConfig.SkBkEnd.ElementAtOrDefault(index));
      yield return new LegacyAskSwitchRange(index + 1, names[index], first, last);
    }
  }

  /// <summary>
  /// Возвращает точки короткой проверки ППУ.
  /// </summary>
  /// <summary>
  /// Возвращает контрольные адреса точек для проверки диапазона БК.
  /// </summary>
  public static IEnumerable<ushort> GetProbeAddresses(LegacyAskSwitchRange range)
  {
    int middleBlock = range.FirstBk + ((range.LastBk - range.FirstBk) / 2);
    foreach (int block in new[] { range.FirstBk, middleBlock, range.LastBk }.Distinct())
    {
      yield return LegacyAskPointAddress.Create(range.Stand, block, 1);
      yield return LegacyAskPointAddress.Create(range.Stand, block, 100);
    }
  }

  public static IEnumerable<int> PpuOneSecondVoltages(int configuredMaximum)
  {
    int maximum = configuredMaximum <= 0 ? 625 : Math.Min(configuredMaximum, 999);
    foreach (int value in new[] { 50, 60, 70, 80, 90, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500 })
    {
      if (value > maximum)
      {
        yield break;
      }

      yield return value;
    }
  }

  /// <summary>
  /// Возвращает эталонные сопротивления ПКИ из legacy-конфигурации.
  /// </summary>
  public static IEnumerable<double> PkiReferenceResistances(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile)
  {
    foreach (double value in profile.HardwareAux.PkiKomTst)
    {
      if (value > 0)
      {
        yield return value * 1_000_000.0;
      }
    }

    yield return 119.8e6;
    yield return 273.8e6;
    yield return 908.58e6;
  }

  /// <summary>
  /// Возвращает диапазоны напряжения ПКИ из таблицы legacy-конфигурации.
  /// </summary>
  public static IEnumerable<(int RangeNumber, double Voltage)> PkiVoltageRanges(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile)
  {
    for (int index = 0; index < profile.HardwareAux.PkiAVolt.Length; index++)
    {
      double voltage = profile.HardwareAux.PkiAVolt[index];
      if (voltage > 0 && voltage <= profile.HardwareConfig.PkiUmax)
      {
        yield return (index + 1, voltage);
      }
    }
  }

  /// <summary>
  /// Возвращает плюсовую шину ПИНТа по правилам старой MKI.
  /// </summary>
  public static ushort GetPintPositiveBus(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile, int pint)
  {
    return pint == 3 ? LegacyAskBus.A2 : LegacyAskBus.A1;
  }

  /// <summary>
  /// Возвращает минусовую шину ПИНТа по правилам старой MKI.
  /// </summary>
  public static ushort GetPintNegativeBus(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile, int pint)
  {
    return pint == 3 ? LegacyAskBus.B2 : LegacyAskBus.B1;
  }

  /// <summary>
  /// Возвращает диапазон напряжения АЦП для выбранного режима.
  /// </summary>
  private static double GetAcpVoltageRange(ushort mode)
  {
    return mode switch
    {
      LegacyAskAcpMode.Voltage1V => 1.0,
      LegacyAskAcpMode.Voltage10V => 10.0,
      _ => 100.0
    };
  }

  /// <summary>
  /// Возвращает диапазон сопротивления АЦП для выбранного режима.
  /// </summary>
  private static double GetAcpResistanceRange(ushort mode)
  {
    return mode switch
    {
      LegacyAskAcpMode.Resistance100Ohm => 100.0,
      LegacyAskAcpMode.Resistance1KOhm => 1000.0,
      LegacyAskAcpMode.Resistance10KOhm => 10000.0,
      _ => 100000.0
    };
  }
}
