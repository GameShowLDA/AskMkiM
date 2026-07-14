using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Самоконтроль цифрового вольтметра старого тестера АСК.
/// </summary>
public sealed partial class LegacyAskDigitalVoltmeterSelfControlTest : LegacyAskModuleTestBase
{
  private const double ShortCircuitResistanceOhm = 240.0;
  private const ushort ShortCircuitRelayBit = 0x0040;
  private const int MultimeterResponseDelayMs = 100;
  private const int MultimeterCommandTimeoutMs = 5000;
  private const int MultimeterMeasurementTimeoutMs = 15000;
  private const string MultimeterMeasureCommand = "READ?";
  private static readonly double[] ResistanceRanges = [100.0, 1000.0, 10000.0, 100000.0];
  private DateTime _summaryStartedAt;
  private TimeSpan _summaryElapsed;
  private bool _summaryIsIdleMode;
  private bool _summaryReady;

  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.DigitalVoltmeter;

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест цифрового вольтметра";
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

    await EnsureMultimeterReadyAsync(context, title, isIdleMode);

    LogInformation($"Самоконтроль АСК: старт теста цифрового мультиметра, холостой режим={isIdleMode}.", isDeviceLog: true);
    await RunZeroVoltageTestAsync(context, controller, title);
    await RunPint4VoltageTestAsync(context, controller, title);
    await RunShortCircuitResistanceTestAsync(context, controller, title);
    LogInformation("Самоконтроль АСК: тест цифрового мультиметра завершен.", isDeviceLog: true);

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

    foreach (var range in ranges)
    {
      await ConfigureVoltageAsync(context, range.NominalValue, "измерение 0В");
      await ConnectVoltmeterToBusAsync(controller, LegacyAskBus.A1, LegacyAskBus.A1, isVoltageMode: true, context.CancellationToken);
      var measured = await MeasureVoltageAsync(context, range.ExpectedValue, range.NominalValue, "измерение 0В");
      await context.Protocol.TestStepAsync($"ДиапU={range.DisplayName} U д.быть=0В+-{range.AbsoluteErrorText}  Uизм={FormatVoltage(measured)}");
    }

    await context.Protocol.EndSubTestAsync(title, 1, testName);
  }

  /// <summary>
  /// Выполняет тест измерения напряжения ПИНТ4.
  /// </summary>
  private static async Task RunPint4VoltageTestAsync(
    LegacyAskSelfControlContext context,
    LegacyAskControllerProtocol controller,
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
      await ConfigureVoltageAsync(context, testCase.Range, "измерение напряжения ПИНТ4");
      await ConnectVoltmeterToBusAsync(controller, testCase.PositiveBus, testCase.NegativeBus, isVoltageMode: true, context.CancellationToken);



      if (testCase.MustBeOverload)
      {
        await CheckVoltageOverloadAsync(context, testCase.ExpectedVoltage, testCase.Range, "проверка перегрузки ПИНТ4");
        await context.Protocol.TestStepAsync(
          $"Uпинт4({FormatBus(testCase.PositiveBus)}+ {FormatBus(testCase.NegativeBus)}-)={FormatVoltageShort(testCase.ExpectedVoltage)} Диап={FormatVoltageShort(testCase.Range)} Д.быть перегр.  Uизм>{FormatVoltageShort(testCase.Range)}");
        continue;
      }

      var measuredVoltage = await MeasureVoltageAsync(context, testCase.ExpectedVoltage, testCase.Range, "измерение напряжения ПИНТ4");
      await context.Protocol.TestStepAsync(
        $"Uпинт4({FormatBus(testCase.PositiveBus)}+ {FormatBus(testCase.NegativeBus)}-) д.быть={testCase.ExpectedText}  Диап={FormatVoltageShort(testCase.Range)}  Uизм={FormatVoltage(measuredVoltage)}");
    }

    await context.Protocol.EndSubTestAsync(title, 2, testName);
  }

  /// <summary>
  /// Выполняет тест измерения сопротивления КЗШ.
  /// </summary>
  private static async Task RunShortCircuitResistanceTestAsync(
    LegacyAskSelfControlContext context,
    LegacyAskControllerProtocol controller,
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

    try
    {
      foreach (var testCase in cases)
      {
        await SetShortCircuitRelayAsync(controller, true, context.CancellationToken);
        await ConfigureResistanceAsync(context, testCase.Range, "измерение сопротивления КЗШ");
        await ConnectVoltmeterToBusAsync(controller, LegacyAskBus.A1, LegacyAskBus.B1, isVoltageMode: false, context.CancellationToken);


        if (testCase.MustBeOverload)
        {
          await CheckResistanceOverloadAsync(context, ShortCircuitResistanceOhm, testCase.Range, "проверка перегрузки сопротивления КЗШ");
          await context.Protocol.TestStepAsync($"Диап={testCase.DisplayRange} R д.быть={ShortCircuitResistanceOhm:0} Ом Д.быть перегр.  Rизм>{testCase.DisplayRange}");
          continue;
        }

        var measuredResistance = await MeasureResistanceAsync(context, ShortCircuitResistanceOhm, testCase.Range, "измерение сопротивления КЗШ");
        await context.Protocol.TestStepAsync(
          $"Диап={testCase.DisplayRange} R д.быть={ShortCircuitResistanceOhm:0} Ом+-{FormatResistanceTolerance(testCase.AbsoluteErrorOhm)}  Rизм={FormatResistance(measuredResistance)}");
      }
    }
    finally
    {
      await SetShortCircuitRelayAsync(controller, false, context.CancellationToken);
    }

    await ConnectVoltmeterToBusAsync(controller, 0, 0, isVoltageMode: true, context.CancellationToken);
    await context.Protocol.EndSubTestAsync(title, 3, testName);
  }

  /// <summary>
  /// Подготавливает выбранный мультиметр к аппаратному тесту.
  /// </summary>
  private static async Task EnsureMultimeterReadyAsync(LegacyAskSelfControlContext context, string title, bool isIdleMode)
  {
    if (isIdleMode)
    {
      LogInformation($"[{title}] Холостой режим: подключение мультиметра не требуется.", isDeviceLog: true);
      return;
    }

    if (context.Multimeter == null)
    {
      throw new InvalidOperationException("Для боевого теста цифрового вольтметра не выбран мультиметр.");
    }

    LogInformation($"[{title}] Инициализация мультиметра {context.Multimeter.Name}({context.Multimeter.NumberChassis}.{context.Multimeter.Number}).", isDeviceLog: true);
    var connection = await context.Multimeter.ConnectableManager.InitializeAsync(context.MessageService);
    LogInformation($"[{title}] Инициализация мультиметра: success={connection.Connect}, answer={connection.Answer}", isDeviceLog: true);

    if (!connection.Connect)
    {
      throw new InvalidOperationException($"Не удалось подключить мультиметр: {connection.Answer}");
    }
  }

  /// <summary>
  /// Выполняет измерение постоянного напряжения или возвращает эмуляцию в холостом режиме.
  /// </summary>
  private static async Task<double> MeasureVoltageAsync(
    LegacyAskSelfControlContext context,
    double expected,
    double range,
    string operation)
  {
    if (context.Multimeter == null)
    {
      LogInformation($"[Тест цифрового вольтметра] {operation}: мультиметр не выбран, используется эмуляция {expected}.", isDeviceLog: true);
      return expected;
    }

    LogInformation($"[Тест цифрового вольтметра] {operation}: DC READ?, диапазон={range}, ожидается={expected}.", isDeviceLog: true);
    double measured = await ReadMultimeterValueAsync(context, GetMeasureCommand(context.Multimeter.DCVCommands.Measure));
    LogInformation($"[Тест цифрового вольтметра] {operation}: измерено={measured}.", isDeviceLog: true);
    return measured;
  }

  /// <summary>
  /// Настраивает мультиметр на измерение постоянного напряжения с заданным пределом.
  /// </summary>
  private static async Task ConfigureVoltageAsync(LegacyAskSelfControlContext context, double range, string operation)
  {
    if (context.Multimeter == null)
    {
      LogInformation($"[Тест цифрового вольтметра] {operation}: холостой режим, настройка DC-диапазона {range} В эмулирована.", isDeviceLog: true);
      return;
    }

    LogInformation($"[Тест цифрового вольтметра] {operation}: установка DC-диапазона {range} В.", isDeviceLog: true);
    await context.Multimeter.DcVoltageManager.SetDCVoltageModeAsync(context.MessageService);
    await SetVoltageRangeAsync(context, range, operation);
    await SetImmediateTriggerAsync(context, operation);
  }

  /// <summary>
  /// Проверяет ожидаемую перегрузку по напряжению без срыва всего теста на штатном ответе прибора.
  /// </summary>
  private static async Task CheckVoltageOverloadAsync(
    LegacyAskSelfControlContext context,
    double expected,
    double range,
    string operation)
  {
    if (context.Multimeter == null)
    {
      LogInformation($"[Тест цифрового вольтметра] {operation}: холостой режим, перегрузка эмулирована.", isDeviceLog: true);
      return;
    }

    try
    {
      LogInformation($"[Тест цифрового вольтметра] {operation}: DC, диапазон={range}, ожидается перегрузка от {expected}.", isDeviceLog: true);
    double measured = await ReadMultimeterValueAsync(context, GetMeasureCommand(context.Multimeter.DCVCommands.Measure));
      if (measured <= range)
      {
        throw new InvalidOperationException($"Мультиметр не вернул перегрузку: измерено {measured}, диапазон {range}.");
      }
    }
    catch (Exception ex) when (IsExpectedOverloadException(ex))
    {
      LogInformation($"[Тест цифрового вольтметра] {operation}: прибор вернул ожидаемую перегрузку: {ex.Message}", isDeviceLog: true);
    }
  }

  /// <summary>
  /// Выполняет измерение сопротивления или возвращает эмуляцию в холостом режиме.
  /// </summary>
  private static async Task<double> MeasureResistanceAsync(
    LegacyAskSelfControlContext context,
    double expected,
    double range,
    string operation)
  {
    if (context.Multimeter == null)
    {
      LogInformation($"[Тест цифрового вольтметра] {operation}: мультиметр не выбран, используется эмуляция {expected}.", isDeviceLog: true);
      return expected;
    }

    LogInformation($"[Тест цифрового вольтметра] {operation}: R READ?, диапазон={range}, ожидается={expected}.", isDeviceLog: true);
    double measured = await ReadMultimeterValueAsync(context, MultimeterMeasureCommand);
    LogInformation($"[Тест цифрового вольтметра] {operation}: измерено={measured}.", isDeviceLog: true);
    return measured;
  }

  /// <summary>
  /// Настраивает мультиметр на измерение сопротивления с заданным пределом.
  /// </summary>
  private static async Task ConfigureResistanceAsync(LegacyAskSelfControlContext context, double range, string operation)
  {
    if (context.Multimeter == null)
    {
      LogInformation($"[Тест цифрового вольтметра] {operation}: холостой режим, настройка R-диапазона {range} Ом эмулирована.", isDeviceLog: true);
      return;
    }

    LogInformation($"[Тест цифрового вольтметра] {operation}: установка R-диапазона {range} Ом.", isDeviceLog: true);
    await context.Multimeter.ResistanceManager.SetResistanceModeAsync(context.MessageService);

    if (!ExecutionConfig.GetIsIdleModeEnabled())
    {
      await ClearMultimeterErrorsAsync(context, operation);

      double supportedRange = ResolveSupportedRange(range, ResistanceRanges);
      string command = string.Format(CultureInfo.InvariantCulture, "CONF:RES {0},{1}", supportedRange, ResolveResistanceResolution(supportedRange));
      string response = await context.Multimeter.DeviceProtocol.QueryAsync(command, timeout: MultimeterCommandTimeoutMs);
      LogInformation($"[Тест цифрового вольтметра] {operation}: диапазон мультиметра {supportedRange} Ом, команда {command}, ответ={response}.", isDeviceLog: true);
      await EnsureMultimeterReadyAfterRangeAsync(context, operation);
      await SetImmediateTriggerAsync(context, operation);
    }
  }

  /// <summary>
  /// Проверяет ожидаемую перегрузку по сопротивлению без срыва всего теста на штатном ответе прибора.
  /// </summary>
}
