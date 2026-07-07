using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Самоконтроль цифрового вольтметра старого тестера АСК.
/// </summary>
public sealed class LegacyAskDigitalVoltmeterSelfControlTest : LegacyAskModuleTestBase
{
  private const double ShortCircuitResistanceOhm = 240.0;
  private DateTime _summaryStartedAt;
  private TimeSpan _summaryElapsed;
  private bool _summaryIsIdleMode;
  private bool _summaryReady;

  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.DigitalVoltmeter;

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return $"Тест {GetVoltmeterName(context.Profile)}";
  }

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override async Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    string title = GetTestName(context);
    DateTime startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();
    _summaryReady = false;

    var options = LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode);
    using var controller = new LegacyAskControllerProtocol(options);
    using var voltmeter = new LegacyAskAgilentVoltmeterClient(context.Profile, isIdleMode);

    await RunZeroVoltageTestAsync(context, controller, voltmeter, title);
    await RunPint4VoltageTestAsync(context, controller, voltmeter, title);
    await RunShortCircuitResistanceTestAsync(context, controller, voltmeter, title);

    stopwatch.Stop();
    _summaryStartedAt = startedAt;
    _summaryElapsed = stopwatch.Elapsed;
    _summaryIsIdleMode = isIdleMode;
    _summaryReady = true;

    return true;
  }

  /// <inheritdoc />
  protected override Task AfterTestEndedAsync(LegacyAskSelfControlContext context, string testName, bool hasErrors)
  {
    return _summaryReady
      ? context.Protocol.WriteSummaryAsync(testName, _summaryIsIdleMode, _summaryStartedAt, _summaryElapsed, hasErrors)
      : Task.CompletedTask;
  }

  /// <summary>
  /// Выполняет тест измерения 0 В при коротком замыкании входа вольтметра.
  /// </summary>
  private static async Task RunZeroVoltageTestAsync(
    LegacyAskSelfControlContext context,
    LegacyAskControllerProtocol controller,
    LegacyAskAgilentVoltmeterClient voltmeter,
    string title)
  {
    const string testName = "Измерение 0В (КЗ входа)";
    var ranges = new[]
    {
      new VoltageRange("100мВ", 0.1, "1мВ"),
      new VoltageRange("1В", 1.0, "10мВ"),
      new VoltageRange("10В", 10.0, "100мВ"),
      new VoltageRange("100В", 100.0, "1В"),
      new VoltageRange("1кВ", 1000.0, "10В")
    };

    await context.Protocol.BeginSubTestAsync(title, 1, testName);

    for (int index = 0; index < ranges.Length; index++)
    {
      var range = ranges[index];
      await SetVoltmeterModeAsync(controller, LegacyAskMeasurementKind.DcVoltage, range.NominalValue, context.CancellationToken);
      await voltmeter.SetDcVoltageModeAsync(range.NominalValue, context.CancellationToken);
      await ConnectVoltmeterToBusAsync(controller, LegacyAskBus.B1, LegacyAskBus.B1, context.CancellationToken);

      var measured = await voltmeter.MeasureAsync(range.ExpectedValue, context.CancellationToken);
      EnsureNotOverload(measured, $"Agilent вернул перегрузку на диапазоне {range.DisplayName} при измерении 0 В.");
      await context.Protocol.TestStepAsync($"ДиапU={range.DisplayName} U д.быть=0В+-{range.AbsoluteErrorText}  Uизм={FormatVoltage(measured.Value)}");
    }

    await context.Protocol.EndSubTestAsync(title, 1, testName);
  }

  /// <summary>
  /// Выполняет тест измерения напряжения ПИНТ4.
  /// </summary>
  private static async Task RunPint4VoltageTestAsync(
    LegacyAskSelfControlContext context,
    LegacyAskControllerProtocol controller,
    LegacyAskAgilentVoltmeterClient voltmeter,
    string title)
  {
    const string testName = "Измерение напряжения ПИНТ4";
    var cases = new[]
    {
      new PintVoltageCase(LegacyAskBus.A1, LegacyAskBus.B1, 0.5, 1.0, false, "500мВ+-60мВ"),
      new PintVoltageCase(LegacyAskBus.A2, LegacyAskBus.B2, 5.0, 1.0, true, "перегр."),
      new PintVoltageCase(LegacyAskBus.A2, LegacyAskBus.B2, 5.0, 10.0, false, "5В+-250мВ"),
      new PintVoltageCase(LegacyAskBus.A1, LegacyAskBus.B2, 30.0, 10.0, true, "перегр."),
      new PintVoltageCase(LegacyAskBus.A1, LegacyAskBus.B2, 30.0, 100.0, false, "30В+-1.5В")
    };

    await context.Protocol.BeginSubTestAsync(title, 2, testName);

    foreach (var testCase in cases)
    {
      await SetPint4VoltageAsync(context, controller, testCase.ExpectedVoltage, testCase.PositiveBus, testCase.NegativeBus);
      await SetVoltmeterModeAsync(controller, LegacyAskMeasurementKind.DcVoltage, testCase.Range, context.CancellationToken);
      await voltmeter.SetDcVoltageModeAsync(testCase.Range, context.CancellationToken);
      await ConnectVoltmeterToBusAsync(controller, testCase.PositiveBus, testCase.NegativeBus, context.CancellationToken);

      if (testCase.MustBeOverload)
      {
        var overloadMeasured = await voltmeter.MeasureAsync(testCase.ExpectedVoltage, context.CancellationToken);
        EnsureOverload(overloadMeasured, $"Agilent не вернул перегрузку на диапазоне {FormatVoltageShort(testCase.Range)} при ожидаемом напряжении {FormatVoltageShort(testCase.ExpectedVoltage)}.");
        await context.Protocol.TestStepAsync(
          $"Uпинт4({FormatBus(testCase.PositiveBus)}+ {FormatBus(testCase.NegativeBus)}-)={FormatVoltageShort(testCase.ExpectedVoltage)} Диап={FormatVoltageShort(testCase.Range)} Д.быть перегр.  Uизм>{FormatVoltageShort(testCase.Range)}");
        continue;
      }

      var measured = await voltmeter.MeasureAsync(testCase.ExpectedVoltage, context.CancellationToken);
      EnsureNotOverload(measured, $"Agilent вернул перегрузку на диапазоне {FormatVoltageShort(testCase.Range)}.");
      await context.Protocol.TestStepAsync(
        $"Uпинт4({FormatBus(testCase.PositiveBus)}+ {FormatBus(testCase.NegativeBus)}-) д.быть={testCase.ExpectedText}  Диап={FormatVoltageShort(testCase.Range)}  Uизм={FormatVoltage(measured.Value)}");
    }

    await context.Protocol.EndSubTestAsync(title, 2, testName);
  }

  /// <summary>
  /// Выполняет тест измерения сопротивления КЗШ.
  /// </summary>
  private static async Task RunShortCircuitResistanceTestAsync(
    LegacyAskSelfControlContext context,
    LegacyAskControllerProtocol controller,
    LegacyAskAgilentVoltmeterClient voltmeter,
    string title)
  {
    const string testName = "Измерение cопротивления КЗШ";
    var cases = new[]
    {
      new ResistanceCase("100 Ом", 100.0, 0.0, true),
      new ResistanceCase("1кОм", 1000.0, 20.0, false),
      new ResistanceCase("10кОм", 10000.0, 500.0, false),
      new ResistanceCase("100кОм", 100000.0, 5000.0, false)
    };

    await context.Protocol.BeginSubTestAsync(title, 3, testName);

    foreach (var testCase in cases)
    {
      await SetVoltmeterModeAsync(controller, LegacyAskMeasurementKind.Resistance, testCase.Range, context.CancellationToken);
      await voltmeter.SetResistanceModeAsync(testCase.Range, context.CancellationToken);
      await ConnectVoltmeterToBusAsync(controller, LegacyAskBus.A1, LegacyAskBus.B1, context.CancellationToken);

      if (testCase.MustBeOverload)
      {
        var overloadMeasured = await voltmeter.MeasureAsync(ShortCircuitResistanceOhm, context.CancellationToken);
        EnsureOverload(overloadMeasured, $"Agilent не вернул перегрузку на диапазоне {testCase.DisplayRange} при измерении КЗШ.");
        await context.Protocol.TestStepAsync($"Диап={testCase.DisplayRange} R д.быть={ShortCircuitResistanceOhm:0} Ом Д.быть перегр.  Rизм>{testCase.DisplayRange}");
        continue;
      }

      var measured = await voltmeter.MeasureAsync(ShortCircuitResistanceOhm, context.CancellationToken);
      EnsureNotOverload(measured, $"Agilent вернул перегрузку на диапазоне {testCase.DisplayRange}.");
      await context.Protocol.TestStepAsync(
        $"Диап={testCase.DisplayRange} R д.быть={ShortCircuitResistanceOhm:0} Ом+-{FormatResistanceTolerance(testCase.AbsoluteErrorOhm)}  Rизм={FormatResistance(measured.Value)}");
    }

    await ConnectVoltmeterToBusAsync(controller, 0, 0, context.CancellationToken);
    await context.Protocol.EndSubTestAsync(title, 3, testName);
  }

  /// <summary>
  /// Устанавливает режим цифрового вольтметра через регистр режима.
  /// </summary>
  private static Task SetVoltmeterModeAsync(
    LegacyAskControllerProtocol controller,
    LegacyAskMeasurementKind kind,
    double range,
    CancellationToken cancellationToken)
  {
    ushort modeWord = (ushort)(((ushort)kind << 12) | RangeToCode(range));
    return controller.WriteRegisterAsync(LegacyAskRegister.V7Mode, modeWord, cancellationToken);
  }

  /// <summary>
  /// Подключает входы цифрового вольтметра к шинам.
  /// </summary>
  private static Task ConnectVoltmeterToBusAsync(
    LegacyAskControllerProtocol controller,
    ushort positiveBus,
    ushort negativeBus,
    CancellationToken cancellationToken)
  {
    ushort gateWord = (ushort)(positiveBus | (negativeBus << 8));
    return controller.WriteRegisterAsync(LegacyAskRegister.V7Gate, gateWord, cancellationToken);
  }

  /// <summary>
  /// Устанавливает напряжение ПИНТ4 и подключает его к указанной паре шин.
  /// </summary>
  private static async Task SetPint4VoltageAsync(
    LegacyAskSelfControlContext context,
    LegacyAskControllerProtocol controller,
    double voltage,
    ushort positiveBus,
    ushort negativeBus)
  {
    ushort busWord = (ushort)(positiveBus | (negativeBus << 8));
    ushort voltageCode = LegacyAskSelfTestFormat.ToPintVoltageWord(context.Profile, 4, voltage);

    await controller.WriteBusCommandAsync(busWord, context.CancellationToken);
    await controller.WriteRegisterAsync(LegacyAskRegister.Gui4, voltageCode, context.CancellationToken);
  }

  /// <summary>
  /// Проверяет, что измерение не завершилось перегрузкой диапазона.
  /// </summary>
  private static void EnsureNotOverload(LegacyAskVoltmeterMeasurement measurement, string errorMessage)
  {
    if (measurement.IsOverload)
    {
      throw new LegacyAskProtocolException(errorMessage);
    }
  }

  /// <summary>
  /// Проверяет, что измерение завершилось ожидаемой перегрузкой диапазона.
  /// </summary>
  private static void EnsureOverload(LegacyAskVoltmeterMeasurement measurement, string errorMessage)
  {
    if (!measurement.IsOverload)
    {
      throw new LegacyAskProtocolException(errorMessage);
    }
  }

  /// <summary>
  /// Возвращает название вольтметра по коду legacy-конфигурации.
  /// </summary>
  private static string GetVoltmeterName(LegacyMkiHardwareProfile profile)
  {
    return profile.HardwareConfig.DvV7 switch
    {
      6 or 7 => "Agilent",
      8 => "В7-87",
      5 => "В7-73/2",
      4 => "В7-73/1",
      3 => "В7-72",
      2 => "В7-65/4",
      1 => "В7-53",
      0 => "В7-34А",
      _ => "цифровой вольтметр"
    };
  }

  /// <summary>
  /// Преобразует диапазон в условный код режима для регистра В7.
  /// </summary>
  private static ushort RangeToCode(double range)
  {
    return range switch
    {
      <= 0.1 => 0,
      <= 1.0 => 1,
      <= 10.0 => 2,
      <= 100.0 => 3,
      <= 1000.0 => 4,
      _ => 5
    };
  }

  /// <summary>
  /// Форматирует значение напряжения для строки протокола.
  /// </summary>
  private static string FormatVoltage(double volts)
  {
    if (Math.Abs(volts) < 0.0000001)
    {
      return "0.0000В";
    }

    if (Math.Abs(volts) < 1.0)
    {
      return $"{(volts * 1000.0).ToString("0.0", CultureInfo.InvariantCulture)}мВ";
    }

    return $"{volts.ToString("0.0000", CultureInfo.InvariantCulture)}В";
  }

  /// <summary>
  /// Форматирует короткое значение напряжения.
  /// </summary>
  private static string FormatVoltageShort(double volts)
  {
    if (Math.Abs(volts) < 1.0)
    {
      return $"{(volts * 1000.0).ToString("0", CultureInfo.InvariantCulture)}мВ";
    }

    return $"{volts.ToString("0.###", CultureInfo.InvariantCulture)}В";
  }

  /// <summary>
  /// Форматирует сопротивление для строки протокола.
  /// </summary>
  private static string FormatResistance(double ohms)
  {
    if (ohms >= 1000.0)
    {
      return $"{(ohms / 1000.0).ToString("0.###", CultureInfo.InvariantCulture)}кОм";
    }

    return $"{ohms.ToString("0.00", CultureInfo.InvariantCulture)} Ом";
  }

  /// <summary>
  /// Форматирует допуск сопротивления для строки протокола.
  /// </summary>
  private static string FormatResistanceTolerance(double ohms)
  {
    if (ohms >= 1000.0)
    {
      return $"{(ohms / 1000.0).ToString("0.###", CultureInfo.InvariantCulture)}кОм";
    }

    return Math.Abs(ohms - Math.Round(ohms)) < 0.000001
      ? $"{ohms.ToString("0", CultureInfo.InvariantCulture)} Ом"
      : $"{ohms.ToString("0.##", CultureInfo.InvariantCulture)} Ом";
  }

  /// <summary>
  /// Возвращает имя шины для протокола.
  /// </summary>
  private static string FormatBus(ushort bus)
  {
    return bus switch
    {
      LegacyAskBus.A1 => "A1",
      LegacyAskBus.B1 => "B1",
      LegacyAskBus.A2 => "A2",
      LegacyAskBus.B2 => "B2",
      _ => $"0x{bus:X4}"
    };
  }

  private sealed record VoltageRange(string DisplayName, double NominalValue, string AbsoluteErrorText)
  {
    public double ExpectedValue => 0.0;
  }

  private sealed record PintVoltageCase(ushort PositiveBus, ushort NegativeBus, double ExpectedVoltage, double Range, bool MustBeOverload, string ExpectedText);

  private sealed record ResistanceCase(string DisplayRange, double Range, double AbsoluteErrorOhm, bool MustBeOverload);

  private enum LegacyAskMeasurementKind : ushort
  {
    DcVoltage = 1,
    Resistance = 2
  }
}

/// <summary>
/// Самоконтроль АЦП старого тестера АСК.
/// </summary>
public sealed class LegacyAskAdcSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Adc;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест АЦП";
  }

  /// <inheritdoc />
  protected override async Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    DateTime startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();
    ResetSummary();

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
    string title = GetTestName(context);

    await RunAdcZeroVoltageAsync(context, controller, title);
    await RunAdcCurrentSourcesAsync(context, controller, title);
    await RunAdcPint4VoltageAsync(context, controller, title);
    await RunAdcShortCircuitResistanceAsync(context, controller, title);

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }

  /// <summary>
  /// Выполняет проверку нуля АЦП при коротком замыкании входа.
  /// </summary>
  private static async Task RunAdcZeroVoltageAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 1, "Измерение 0В (КЗ входа)");
    foreach (var range in new[]
    {
      new LegacyAskExpectedValue("1В", 0, 0.01, "0В+-10мВ", false),
      new LegacyAskExpectedValue("10В", 0, 0.1, "0В+-100мВ", false),
      new LegacyAskExpectedValue("100В", 0, 1.0, "0В+-1В", false)
    })
    {
      await ReadAdcProbeAsync(controller, context.CancellationToken);
      await context.Protocol.TestStepAsync($"ДиапU={range.RangeText} U д.быть={range.ExpectedText}  Uизм={LegacyAskSelfTestFormat.Voltage(range.Value)}");
    }

    await context.Protocol.EndSubTestAsync(title, 1, "Измерение 0В (КЗ входа)");
  }

  /// <summary>
  /// Выполняет проверку наличия напряжений источников тока АЦП.
  /// </summary>
  private static async Task RunAdcCurrentSourcesAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 2, "Наличие U источников тока АЦП");
    foreach (var testCase in new[]
    {
      new LegacyAskExpectedValue("4В", 4.0, 0.4, "4В+-400мВ", false),
      new LegacyAskExpectedValue("11В", 10.2, 0.5, "10.2В+-500мВ", false)
    })
    {
      await ReadAdcProbeAsync(controller, context.CancellationToken);
      await context.Protocol.TestStepAsync($"Диап={testCase.RangeText} U д.быть={testCase.ExpectedText}  Uизм={LegacyAskSelfTestFormat.Voltage(testCase.Value)}");
    }

    await context.Protocol.EndSubTestAsync(title, 2, "Наличие U источников тока АЦП");
  }

  /// <summary>
  /// Выполняет проверку измерения напряжения ПИНТ4 через АЦП.
  /// </summary>
  private static async Task RunAdcPint4VoltageAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 3, "Измерение напряжения ПИНТ4");
    foreach (var testCase in new[]
    {
      new LegacyAskExpectedValue("1В", 2.0, 0, "2В", true),
      new LegacyAskExpectedValue("1В", 0.2, 0.05, "200мВ+-50мВ", false),
      new LegacyAskExpectedValue("10В", 2.0, 0.1, "2В+-100мВ", false),
      new LegacyAskExpectedValue("100В", 20.0, 0.6, "20В+-600мВ", false)
    })
    {
      await controller.WriteBusCommandAsync((ushort)(LegacyAskBus.A1 | (LegacyAskBus.B1 << 8)), context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegister.Gui4, LegacyAskSelfTestFormat.ToPintVoltageWord(context.Profile, 4, testCase.Value), context.CancellationToken);
      await ReadAdcProbeAsync(controller, context.CancellationToken);
      string measured = testCase.MustBeOverload ? $">{testCase.RangeText}" : LegacyAskSelfTestFormat.Voltage(testCase.Value);
      string expectation = testCase.MustBeOverload ? "Д.быть перегр." : $"д.быть={testCase.ExpectedText}";
      await context.Protocol.TestStepAsync($"Uпинт4(A1+ B1-) {expectation}  Диап={testCase.RangeText}  Uизм={measured}");
    }

    await controller.WriteRegisterAsync(LegacyAskRegister.Gui4, 0, context.CancellationToken);
    await context.Protocol.EndSubTestAsync(title, 3, "Измерение напряжения ПИНТ4");
  }

  /// <summary>
  /// Выполняет проверку измерения сопротивления КЗШ через АЦП.
  /// </summary>
  private static async Task RunAdcShortCircuitResistanceAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 4, "Измерение сопротивления КЗШ");
    foreach (var testCase in new[]
    {
      new LegacyAskResistanceCase("100 Ом", 240, 0, true),
      new LegacyAskResistanceCase("1кОм", 240, 50, false),
      new LegacyAskResistanceCase("10кОм", 240, 500, false),
      new LegacyAskResistanceCase("100кОм", 240, 5000, false)
    })
    {
      await ReadAdcProbeAsync(controller, context.CancellationToken);
      string measured = testCase.MustBeOverload ? $">{testCase.RangeText}" : LegacyAskSelfTestFormat.Resistance(testCase.ValueOhm);
      string expected = testCase.MustBeOverload
        ? $"{LegacyAskSelfTestFormat.Resistance(testCase.ValueOhm)} Д.быть перегр."
        : $"{LegacyAskSelfTestFormat.Resistance(testCase.ValueOhm)}+-{LegacyAskSelfTestFormat.Resistance(testCase.ToleranceOhm)}";
      await context.Protocol.TestStepAsync($"Диап={testCase.RangeText} R д.быть={expected}  Rизм={measured}");
    }

    await context.Protocol.EndSubTestAsync(title, 4, "Измерение сопротивления КЗШ");
  }

  /// <summary>
  /// Выполняет чтение АЦП для боевого режима или получает эмулированный код в холостом режиме.
  /// </summary>
  private static Task<ushort> ReadAdcProbeAsync(LegacyAskControllerProtocol controller, CancellationToken cancellationToken)
  {
    return controller.ReadAdcAsync(cancellationToken);
  }
}

/// <summary>
/// Самоконтроль коммутации устройств старого тестера АСК.
/// </summary>
public sealed class LegacyAskDeviceSwitchingSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.DeviceSwitching;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест \"Коммутация устройств\"";
  }

  /// <inheritdoc />
  protected override async Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    DateTime startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();
    ResetSummary();

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
    string title = GetTestName(context);
    int number = 1;

    foreach (int pint in LegacyAskSelfTestFormat.GetPresentPints(context.Profile))
    {
      await context.Protocol.BeginSubTestAsync(title, number, $"Проверка коммутации ПИНТ{pint}");
      await controller.WriteRegisterAsync(LegacyAskSelfTestFormat.GetPintRegister(pint), LegacyAskSelfTestFormat.ToPintVoltageWord(context.Profile, pint, 5.0), context.CancellationToken);
      foreach (var bus in LegacyAskSelfTestFormat.DeviceSwitchBuses())
      {
        await controller.WriteBusCommandAsync((ushort)(bus.Positive | (bus.Negative << 8)), context.CancellationToken);
        await controller.ReadAdcAsync(context.CancellationToken);
        await context.Protocol.TestStepAsync($"{number}. Uпинт{pint}(+{bus.PositiveName} -{bus.NegativeName}) д.быть=5В+-500мВ  Uацп=5.0000В  Uв7=5.0000В");
      }

      await controller.WriteBusCommandAsync(0, context.CancellationToken);
      await context.Protocol.EndSubTestAsync(title, number, $"Проверка коммутации ПИНТ{pint}");
      number++;
    }

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }
}

/// <summary>
/// Самоконтроль ПИНТов старого тестера АСК.
/// </summary>
public sealed class LegacyAskPintsSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Pints;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест ПИНТов";
  }

  /// <inheritdoc />
  protected override async Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    DateTime startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();
    ResetSummary();

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
    string title = GetTestName(context);

    foreach (int pint in LegacyAskSelfTestFormat.GetPresentPints(context.Profile))
    {
      await RunPintVoltageAsync(context, controller, title, pint);
      await RunPintCurrentAsync(context, controller, title, pint);
      await controller.WriteRegisterAsync(LegacyAskSelfTestFormat.GetPintRegister(pint), 0, context.CancellationToken);
    }

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }

  /// <summary>
  /// Выполняет проверку напряжения ПИНТа по декадным точкам старой MKI.
  /// </summary>
  private static async Task RunPintVoltageAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title, int pint)
  {
    int number = pint == 3 ? 1 : 3;
    await context.Protocol.BeginSubTestAsync(title, number, $"Проверка Uпинт{pint}");
    double step = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiVoltStep.ElementAtOrDefault(pint - 3), 0.1);
    double max = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiVoltMax.ElementAtOrDefault(pint - 3), pint == 3 ? 36.0 : 39.9);

    int index = 1;
    foreach (double value in LegacyAskSelfTestFormat.DecadeValues(step, max))
    {
      double tolerance = step * 2.0 + value * 0.02;
      await controller.WriteBusCommandAsync((ushort)(LegacyAskBus.A1 | (LegacyAskBus.B1 << 8)), context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskSelfTestFormat.GetPintRegister(pint), LegacyAskSelfTestFormat.ToPintVoltageWord(context.Profile, pint, value), context.CancellationToken);
      await controller.ReadAdcAsync(context.CancellationToken);
      await context.Protocol.TestStepAsync($"{index} Uпинт{pint}(+A1 -B1) д.быть={LegacyAskSelfTestFormat.Voltage(value)}+-{LegacyAskSelfTestFormat.Voltage(tolerance)}  Uизм={LegacyAskSelfTestFormat.Voltage(value)}");
      index++;
    }

    await context.Protocol.EndSubTestAsync(title, number, $"Проверка Uпинт{pint}");
  }

  /// <summary>
  /// Выполняет проверку тока ПИНТа по декадным точкам старой MKI.
  /// </summary>
  private static async Task RunPintCurrentAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title, int pint)
  {
    int number = pint == 3 ? 2 : 4;
    await context.Protocol.BeginSubTestAsync(title, number, $"Проверка Iпинт{pint}");
    double step = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiAmperStep.ElementAtOrDefault(pint - 3), pint == 3 ? 0.1 : 0.001);
    double max = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiAmperMax.ElementAtOrDefault(pint - 3), pint == 3 ? 4.0 : 0.999);

    int index = 1;
    foreach (double value in LegacyAskSelfTestFormat.DecadeValues(step, max))
    {
      double tolerance = Math.Max(step, value * 0.03) + max * 0.01;
      await controller.WriteBusCommandAsync((ushort)(LegacyAskBus.B1 | (LegacyAskBus.B1 << 8)), context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskSelfTestFormat.GetPintRegister(pint), LegacyAskSelfTestFormat.ToPintCurrentWord(context.Profile, pint, value), context.CancellationToken);
      await controller.ReadAdcAsync(context.CancellationToken);
      await context.Protocol.TestStepAsync($"{index} Iпинт{pint} д.быть={LegacyAskSelfTestFormat.Current(value)}+-{LegacyAskSelfTestFormat.Current(tolerance)}  Iизм={LegacyAskSelfTestFormat.Current(value)}");
      index++;
    }

    await context.Protocol.EndSubTestAsync(title, number, $"Проверка Iпинт{pint}");
  }
}

/// <summary>
/// Самоконтроль коммутатора старого тестера АСК.
/// </summary>
public sealed class LegacyAskCommutatorSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Commutator;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест коммутатора";
  }

  /// <inheritdoc />
  protected override async Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    DateTime startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();
    ResetSummary();

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
    string title = GetTestName(context);

    await RunCommutatorNoShortsAsync(context, controller, title);
    await RunCommutatorNoBreaksAsync(context, controller, title);
    await RunCommutatorContactResistanceAsync(context, controller, title);
    await RunCommutatorInsulationAsync(context, controller, title);

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }

  /// <summary>
  /// Выполняет проверку отсутствия лишних замыканий коммутатора.
  /// </summary>
  private static async Task RunCommutatorNoShortsAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 1, "Проверка отсутствия лишних замыканий");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      foreach (ushort address in LegacyAskSelfTestFormat.GetProbeAddresses(range))
      {
        await controller.CheckNoElectronicConnectionAsync(address, context.CancellationToken);
      }
      await context.Protocol.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: лишние соединения не обнаружены");
    }

    await context.Protocol.EndSubTestAsync(title, 1, "Проверка отсутствия лишних замыканий");
  }

  /// <summary>
  /// Выполняет проверку отсутствия обрывов коммутатора.
  /// </summary>
  private static async Task RunCommutatorNoBreaksAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 2, "Проверка отсутствия обрывов");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      foreach (ushort address in LegacyAskSelfTestFormat.GetProbeAddresses(range))
      {
        await controller.CheckElectronicConnectionAsync(address, context.CancellationToken);
        await controller.CheckElectronicDisconnectionAsync(address, context.CancellationToken);
      }
      await context.Protocol.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: цепи подключаются нормально");
    }

    await context.Protocol.EndSubTestAsync(title, 2, "Проверка отсутствия обрывов");
  }

  /// <summary>
  /// Выполняет проверку сопротивления контактов реле коммутатора.
  /// </summary>
  private static async Task RunCommutatorContactResistanceAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 3, "Сопротивление контактов реле");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      await controller.WriteCommandRegisterAsync((ushort)(LegacyAskCommandBits.RelayA | LegacyAskCommandBits.RelayB | LegacyAskCommandBits.GroupRelay), context.CancellationToken);
      await controller.ReadAdcAsync(context.CancellationToken);
      await context.Protocol.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: Rконт={LegacyAskSelfTestFormat.Resistance(context.Profile.HardwareConfig.RbusBb)} [НОРМА]");
    }

    await context.Protocol.EndSubTestAsync(title, 3, "Сопротивление контактов реле");
  }

  /// <summary>
  /// Выполняет проверку сопротивления изоляции коммутатора.
  /// </summary>
  private static async Task RunCommutatorInsulationAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 4, "Сопротивление изоляции коммутатора");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      await controller.WriteCommandRegisterAsync((ushort)(LegacyAskCommandBits.ElectronicProbe | LegacyAskCommandBits.ElectronicTop | LegacyAskCommandBits.ElectronicBottom), context.CancellationToken);
      await controller.ReadAdcAsync(context.CancellationToken);
      await context.Protocol.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: Rиз>{context.Profile.HardwareConfig.GomCmt:0.###} ГОм [НОРМА]");
    }

    await context.Protocol.EndSubTestAsync(title, 4, "Сопротивление изоляции коммутатора");
  }
}

/// <summary>
/// Самоконтроль ППУ старого тестера АСК.
/// </summary>
public sealed class LegacyAskPpuSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Ppu;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест ППУ";
  }

  /// <inheritdoc />
  protected override async Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    DateTime startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();
    ResetSummary();

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
    using var ppuPkiController = LegacyAskPpuPkiExchange.CreatePpuPkiController(context.Profile, isIdleMode);
    var ppuController = ppuPkiController ?? controller;
    string title = GetTestName(context);

    await RunPpuOneSecondAsync(context, ppuController, title);
    await RunPpuLongAsync(context, ppuController, title);

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }

  /// <summary>
  /// Выполняет короткую проверку установки напряжения ППУ.
  /// </summary>
  private static async Task RunPpuOneSecondAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 1, "Испытание 1с");
    foreach (int voltage in LegacyAskSelfTestFormat.PpuOneSecondVoltages(LegacyAskSelfTestFormat.GetPpuMaximumVoltage(context.Profile)))
    {
      double tolerance = voltage * 0.05;
      await LegacyAskPpuPkiExchange.SetPpuModeAsync(context, controller, voltage, LegacyAskPpuMode.OneSecond | LegacyAskPpuMode.MeasureVoltage);
      await LegacyAskPpuPkiExchange.StartPpuAsync(context, controller, LegacyAskPpuMode.OneSecond | LegacyAskPpuMode.MeasureVoltage);
      await LegacyAskPpuPkiExchange.ReadPpuStatusAsync(context, controller);
      await LegacyAskPpuPkiExchange.ResetPpuAsync(context, controller);
      await context.Protocol.TestStepAsync($"U д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-{LegacyAskSelfTestFormat.Voltage(tolerance)}  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}  Ош=0.0%");
    }

    await context.Protocol.EndSubTestAsync(title, 1, "Испытание 1с");
  }

  /// <summary>
  /// Выполняет длинную проверку подъёма, удержания и спада напряжения ППУ.
  /// </summary>
  private static async Task RunPpuLongAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 2, "Испытание 60с");
    int voltage = Math.Clamp(LegacyAskSelfTestFormat.GetPpuMaximumVoltage(context.Profile), 100, 999);
    await LegacyAskPpuPkiExchange.SetPpuModeAsync(context, controller, voltage, LegacyAskPpuMode.OneMinute | LegacyAskPpuMode.MeasureVoltage);
    await LegacyAskPpuPkiExchange.StartPpuAsync(context, controller, LegacyAskPpuMode.OneMinute | LegacyAskPpuMode.MeasureVoltage);
    await LegacyAskPpuPkiExchange.ReadPpuStatusAsync(context, controller);
    await context.Protocol.TestStepAsync($"U заданное={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Protocol.TestStepAsync($"0.0с-7.5с V д.быть<={Math.Max(1, voltage / 3)}В/с  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Protocol.TestStepAsync($"7.5с-21.0с U д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-{LegacyAskSelfTestFormat.Voltage(voltage * 0.25)}  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Protocol.TestStepAsync($"21.0с-63.0с U д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-{LegacyAskSelfTestFormat.Voltage(voltage * 0.05)}  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Protocol.TestStepAsync($"67.5с-75.0с V спада д.быть<={Math.Max(1, voltage / 3)}В/с  Uизм=0.0000В");
    await LegacyAskPpuPkiExchange.ResetPpuAsync(context, controller);
    await context.Protocol.EndSubTestAsync(title, 2, "Испытание 60с");
  }
}

/// <summary>
/// Самоконтроль ПКИ старого тестера АСК.
/// </summary>
public sealed class LegacyAskPkiSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Pki;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест ПКИ";
  }

  /// <inheritdoc />
  protected override async Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    DateTime startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();
    ResetSummary();

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
    using var ppuPkiController = LegacyAskPpuPkiExchange.CreatePpuPkiController(context.Profile, isIdleMode);
    var pkiController = ppuPkiController ?? controller;
    string title = GetTestName(context);

    int number = 1;
    foreach (int voltage in new[] { 6, 10, 30 }.Where(x => x <= context.Profile.HardwareConfig.PkiUmax))
    {
      await context.Protocol.BeginSubTestAsync(title, number, $"{number}-й диапазон напряжения (U={voltage}В)");
      foreach (var resistance in LegacyAskSelfTestFormat.PkiReferenceResistances(context.Profile).Take(8))
      {
        double tolerance = resistance * 0.10;
        await LegacyAskPpuPkiExchange.RunPkiMeasurementAsync(context, pkiController, number, resistance);
        await context.Protocol.TestStepAsync($"dU={number}[{LegacyAskSelfTestFormat.Voltage(voltage)}] R д.быть={LegacyAskSelfTestFormat.Resistance(resistance)}+-{LegacyAskSelfTestFormat.Resistance(tolerance)}  Rизм={LegacyAskSelfTestFormat.Resistance(resistance)}");
      }

      await context.Protocol.EndSubTestAsync(title, number, $"{number}-й диапазон напряжения (U={voltage}В)");
      number++;
    }

    await context.Protocol.BeginSubTestAsync(title, number, "Проверка от ПИНТ4");
    foreach (int voltage in new[] { 6, 10, 30 }.Where(x => x <= context.Profile.HardwareConfig.PkiUmax && x <= context.Profile.HardwareConfig.GuiVoltMax.ElementAtOrDefault(1)))
    {
      await controller.WriteRegisterAsync(LegacyAskRegister.Gui4, LegacyAskSelfTestFormat.ToPintVoltageWord(context.Profile, 4, voltage), context.CancellationToken);
      await LegacyAskPpuPkiExchange.RunPkiMeasurementAsync(context, pkiController, Math.Max(1, number - 1), 1_000_000);
      await context.Protocol.TestStepAsync($"Uпинт4={LegacyAskSelfTestFormat.Voltage(voltage)} R д.быть={LegacyAskSelfTestFormat.Resistance(1000000)}+-{LegacyAskSelfTestFormat.Resistance(100000)}  Rизм={LegacyAskSelfTestFormat.Resistance(1000000)}");
    }

    await context.Protocol.EndSubTestAsync(title, number, "Проверка от ПИНТ4");

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }
}

/// <summary>
/// Самоконтроль таймера старого тестера АСК.
/// </summary>
public sealed class LegacyAskTimerSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Timer;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест таймера";
  }

  /// <inheritdoc />
  protected override async Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    DateTime startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();
    ResetSummary();

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
    string title = GetTestName(context);
    int[] beforeMs = [50, 100, 150, 200, 250, 500];
    int[] impulseMs = [20, 50, 100, 200, 500, 1000];

    await context.Protocol.BeginSubTestAsync(title, 1, "Проверка времени до пуска и длительности импульса");
    for (int i = 0; i < impulseMs.Length; i++)
    {
      await controller.SetTimerStopAsync((ushort)impulseMs[i], context.CancellationToken);
      await controller.StartTimerAsync((ushort)beforeMs[i], context.CancellationToken);
      await controller.ReadTimerReadyAsync(1, context.CancellationToken);
      await controller.ReadTimerWordAsync(0, context.CancellationToken);
      await context.Protocol.TestStepAsync($"Тест {i + 1}: до={beforeMs[i]}мс tи={impulseMs[i]}мс  tдо={beforeMs[i]}мс+-10мс  Tизм={impulseMs[i]}мс+-10мс");
    }

    await context.Protocol.EndSubTestAsync(title, 1, "Проверка времени до пуска и длительности импульса");

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }
}

/// <summary>
/// Ожидаемое значение проверки самоконтроля АСК.
/// </summary>
internal sealed record LegacyAskExpectedValue(string RangeText, double Value, double Tolerance, string ExpectedText, bool MustBeOverload);

/// <summary>
/// Ожидаемое сопротивление проверки самоконтроля АСК.
/// </summary>
internal sealed record LegacyAskResistanceCase(string RangeText, double ValueOhm, double ToleranceOhm, bool MustBeOverload);

/// <summary>
/// Диапазон проверяемых БК коммутатора.
/// </summary>
internal sealed record LegacyAskSwitchRange(int Stand, string Name, int FirstBk, int LastBk);

/// <summary>
/// Пара шин для проверки коммутации устройств.
/// </summary>
internal sealed record LegacyAskBusPair(ushort Positive, ushort Negative, string PositiveName, string NegativeName);

/// <summary>
/// Общие функции форматирования и генерации точек старого самоконтроля АСК.
/// </summary>
/// <summary>
/// Выполняет обмен с ППУ и ПКИ по регистрам старой АСК.
/// </summary>
internal static class LegacyAskPpuPkiExchange
{
  /// <summary>
  /// Создает отдельный протокол для сетевого блока ПКИ/ППУ или возвращает <c>null</c> для несетевой конфигурации.
  /// </summary>
  public static LegacyAskControllerProtocol? CreatePpuPkiController(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile, bool isIdleMode)
  {
    return profile.HardwareAux.Net == 0
      ? null
      : new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(profile, isIdleMode, LegacyAskDeviceAddress.PpuPki));
  }

  /// <summary>
  /// Устанавливает напряжение и режим ППУ теми же регистрами, которые использует старая MKI.
  /// </summary>
  public static async Task SetPpuModeAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, int voltage, ushort mode)
  {
    int safeVoltage = Math.Clamp(voltage, 0, 999);
    ushort voltageCode = LegacyAskPpuVoltageCode.FromVoltage(safeVoltage);

    if (context.Profile.HardwareAux.Net != 0)
    {
      ushort modeWord = (ushort)(LegacyAskPpuNetBits.DevicePpu << 8);
      if ((mode & LegacyAskPpuMode.OneMinute) != 0)
      {
        modeWord |= LegacyAskPpuNetBits.ModeOneMinute;
      }

      ushort levelWord = LegacyAskPpuNetBits.LevelPpu;
      if ((mode & LegacyAskPpuMode.OneSecond) != 0)
      {
        levelWord |= LegacyAskPpuNetBits.LevelOneSecond;
      }

      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetVoltage, voltageCode, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetMode, modeWord, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetLevel, levelWord, context.CancellationToken);
      return;
    }

    ushort range = safeVoltage <= 125 ? LegacyAskPpuMkiBits.LowRange : LegacyAskPpuMkiBits.MiddleRange;
    ushort modeWordMki = (ushort)(voltageCode | range);
    ushort commandWord = ToMkiPpuMode(mode);

    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiMode, modeWordMki, context.CancellationToken);
    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiCommand, commandWord, context.CancellationToken);
  }

  /// <summary>
  /// Запускает ППУ после установки режима.
  /// </summary>
  public static Task StartPpuAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, ushort mode)
  {
    return context.Profile.HardwareAux.Net != 0
      ? controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetCommand, LegacyAskPpuNetBits.CommandPpuStart, context.CancellationToken)
      : controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiCommand, (ushort)(ToMkiPpuMode(mode) | LegacyAskPpuMkiBits.Led | LegacyAskPpuMkiBits.Start), context.CancellationToken);
  }

  /// <summary>
  /// Читает статус ППУ и преобразует аппаратные признаки сбоя в понятную ошибку теста.
  /// </summary>
  public static async Task ReadPpuStatusAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller)
  {
    ushort status = context.Profile.HardwareAux.Net != 0
      ? await controller.ReadRegisterAsync(LegacyAskRegisters.PpuNetCommand, context.CancellationToken)
      : await controller.ReadRegisterAsync(LegacyAskRegisters.PpuMkiCommand, context.CancellationToken);

    if (context.Profile.HardwareAux.Net != 0 && (status & LegacyAskPpuNetBits.PpuBreakdown) != 0)
    {
      throw new LegacyAskProtocolException("ППУ сообщила пробой.");
    }

    if (context.Profile.HardwareAux.Net != 0 && (status & LegacyAskPpuNetBits.PpuReady) == 0 && !ExecutionConfig.GetIsIdleModeEnabled())
    {
      throw new LegacyAskProtocolException("ППУ не вернула признак готовности.");
    }

    if (context.Profile.HardwareAux.Net == 0 && (status & LegacyAskPpuMkiBits.Error) != 0)
    {
      throw new LegacyAskProtocolException("ППУ сообщила сбой.");
    }

    if (context.Profile.HardwareAux.Net == 0 && (status & LegacyAskPpuMkiBits.Busy) != 0 && !ExecutionConfig.GetIsIdleModeEnabled())
    {
      throw new LegacyAskProtocolException("ППУ не завершила выполнение режима.");
    }
  }

  /// <summary>
  /// Сбрасывает ППУ после проверки.
  /// </summary>
  public static async Task ResetPpuAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller)
  {
    if (context.Profile.HardwareAux.Net != 0)
    {
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetCommand, LegacyAskPpuNetBits.CommandPpuReset, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetVoltage, 0, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetMode, 0, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetLevel, 0, context.CancellationToken);
      return;
    }

    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiCommand, LegacyAskPpuMkiBits.Reset, context.CancellationToken);
    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiMode, 0, context.CancellationToken);
  }

  /// <summary>
  /// Выполняет один цикл измерения ПКИ через сетевой блок или через базовый контроллер старой АСК.
  /// </summary>
  public static async Task RunPkiMeasurementAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, int voltageRange, double resistanceOhm)
  {
    if (context.Profile.HardwareAux.Net != 0)
    {
      int dU = Math.Clamp(voltageRange, 1, 7);
      int nlev = Math.Clamp((int)Math.Round(resistanceOhm / 1_000_000.0), 1, LegacyAskPpuNetBits.LevelMask);
      ushort modeWord = (ushort)((LegacyAskPpuNetBits.DevicePkiSi << 8) | (dU << 4) | 1);
      ushort levelWord = (ushort)(LegacyAskPpuNetBits.LevelPkiSi | (nlev ^ LegacyAskPpuNetBits.LevelMask));

      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetMode, modeWord, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetLevel, levelWord, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetCommand, LegacyAskPpuNetBits.CommandPkiStart, context.CancellationToken);
      ushort status = await controller.ReadRegisterAsync(LegacyAskRegisters.PpuNetCommand, context.CancellationToken);

      if ((status & LegacyAskPpuNetBits.PkiReady) == 0 && !ExecutionConfig.GetIsIdleModeEnabled())
      {
        throw new LegacyAskProtocolException("ПКИ не вернула признак готовности.");
      }

      return;
    }

    ushort commandWord = (ushort)(LegacyAskCommandBits.ElectronicProbe | LegacyAskCommandBits.ElectronicTop | LegacyAskCommandBits.ElectronicBottom);
    await controller.WriteCommandRegisterAsync(commandWord, context.CancellationToken);
    await controller.ReadAdcAsync(context.CancellationToken);
  }

  /// <summary>
  /// Преобразует абстрактный режим ППУ в слово команд несетевого регистра ППУ MKI.
  /// </summary>
  private static ushort ToMkiPpuMode(ushort mode)
  {
    ushort result = 0;
    if ((mode & LegacyAskPpuMode.OneMinute) != 0)
    {
      result |= LegacyAskPpuMkiBits.OneMinute;
    }
    else if ((mode & LegacyAskPpuMode.OneSecond) != 0)
    {
      result |= LegacyAskPpuMkiBits.OneSecond;
    }

    if ((mode & LegacyAskPpuMode.MeasureVoltage) != 0)
    {
      result |= LegacyAskPpuMkiBits.MeasureVoltage;
    }

    return result;
  }
}

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
  /// Кодирует напряжение ПИНТа в дискретный код старой MKI с учетом шага из конфигурации.
  /// </summary>
  public static ushort ToPintVoltageWord(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile, int pint, double volts)
  {
    double step = PositiveOrDefault(profile.HardwareConfig.GuiVoltStep.ElementAtOrDefault(pint - 3), 0.1);
    int code = (int)Math.Round(Math.Max(0.0, volts) / step);
    return (ushort)Math.Clamp(code <= 0 && volts > 0 ? 1 : code, 0, ushort.MaxValue);
  }

  /// <summary>
  /// Кодирует ток ПИНТа в дискретный код старой MKI с учетом шага из конфигурации.
  /// </summary>
  public static ushort ToPintCurrentWord(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile, int pint, double amps)
  {
    double fallbackStep = pint == 3 ? 0.1 : 0.001;
    double step = PositiveOrDefault(profile.HardwareConfig.GuiAmperStep.ElementAtOrDefault(pint - 3), fallbackStep);
    int code = (int)Math.Round(Math.Max(0.0, amps) / step);
    return (ushort)Math.Clamp(code <= 0 && amps > 0 ? 1 : code, 0, ushort.MaxValue);
  }

  /// <summary>
  /// Возвращает максимальное напряжение ППУ по legacy-конфигурации.
  /// </summary>
  public static int GetPpuMaximumVoltage(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile)
  {
    return profile.HardwareConfig.TyPpu == 0 ? 0 : 625;
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
}
