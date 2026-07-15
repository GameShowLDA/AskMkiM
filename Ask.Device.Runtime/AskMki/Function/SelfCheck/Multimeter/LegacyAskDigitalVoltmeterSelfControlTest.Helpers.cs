using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Самоконтроль цифрового вольтметра старого тестера АСК.
/// </summary>

public sealed partial class LegacyAskDigitalVoltmeterSelfControlTest
{
  private static async Task<(bool Passed, string MeasuredText)> CheckResistanceOverloadAsync(
    LegacyAskSelfControlContext context,
    double expected,
    double range,
    string operation)
  {
    if (context.Multimeter == null)
    {
      LogInformation($"[Тест цифрового вольтметра] {operation}: холостой режим, перегрузка эмулирована.", isDeviceLog: true);
      return (true, $">{FormatResistance(range)}");
    }

    try
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        LogInformation($"[Тест цифрового вольтметра] {operation}: холостой режим, перегрузка эмулирована.", isDeviceLog: true);
        return (true, $">{FormatResistance(range)}");
      }

      LogInformation($"[Тест цифрового вольтметра] {operation}: R, диапазон={range}, ожидается перегрузка от {expected}.", isDeviceLog: true);
      double measured = await ReadMultimeterValueAsync(context, MultimeterMeasureCommand);
      LogInformation($"[Тест цифрового вольтметра] {operation}: ответ прибора={measured}.", isDeviceLog: true);
      bool passed = measured > range;
      return (passed, passed ? $">{FormatResistance(range)}" : $"={FormatResistance(measured)}");
    }
    catch (Exception ex) when (IsExpectedOverloadException(ex))
    {
      LogInformation($"[Тест цифрового вольтметра] {operation}: прибор вернул ожидаемую перегрузку: {ex.Message}", isDeviceLog: true);
      return (true, $">{FormatResistance(range)}");
    }
  }

  /// <summary>
  /// Читает числовой ответ мультиметра с учетом тайм-аута профиля.
  /// </summary>
  private static async Task<double> ReadMultimeterValueAsync(LegacyAskSelfControlContext context, string command)
  {
    if (context.Multimeter == null)
    {
      throw new InvalidOperationException("Мультиметр не выбран.");
    }

    string response = await context.Multimeter.DeviceProtocol.QueryAsync(command, responseDelay: MultimeterResponseDelayMs, timeout: MultimeterMeasurementTimeoutMs);
    LogInformation($"[Тест цифрового вольтметра] команда {command}, ответ={response}.", isDeviceLog: true);
    return TryParseMeasurement(response, out double measured)
      ? measured
      : throw new InvalidOperationException($"Мультиметр вернул некорректный ответ: {response}");
  }

  /// <summary>
  /// Явно устанавливает диапазон постоянного напряжения из сценария теста.
  /// </summary>
  private static async Task SetVoltageRangeAsync(LegacyAskSelfControlContext context, double range, string operation)
  {
    if (context.Multimeter == null || ExecutionConfig.GetIsIdleModeEnabled())
    {
      return;
    }

    double supportedRange = ResolveSupportedRange(range, context.Multimeter.DCVCommands.SupportedRanges);
    string command = string.Format(
      CultureInfo.InvariantCulture,
      context.Multimeter.DCVCommands.SetRange,
      supportedRange,
      ResolveVoltageResolution(supportedRange));

    await ClearMultimeterErrorsAsync(context, operation);

    string response = await context.Multimeter.DeviceProtocol.QueryAsync(command, timeout: MultimeterCommandTimeoutMs);
    LogInformation($"[Тест цифрового вольтметра] {operation}: диапазон мультиметра {supportedRange} В, команда {command}, ответ={response}.", isDeviceLog: true);
    if (!string.IsNullOrWhiteSpace(context.Multimeter.DCVCommands.GetRangeError))
    {
      string error = await context.Multimeter.DeviceProtocol.QueryAsync(context.Multimeter.DCVCommands.GetRangeError, timeout: MultimeterCommandTimeoutMs);
      LogInformation($"[Тест цифрового вольтметра] {operation}: ошибка диапазона={error}.", isDeviceLog: true);
      if (!string.IsNullOrWhiteSpace(error) && !error.TrimStart().StartsWith("+0", StringComparison.Ordinal))
      {
        throw new InvalidOperationException($"Ошибка установки диапазона мультиметра: {error}");
      }
    }
  }

  /// <summary>
  /// Проверяет состояние мультиметра после установки диапазона.
  /// </summary>
  private static async Task EnsureMultimeterReadyAfterRangeAsync(LegacyAskSelfControlContext context, string operation)
  {
    if (context.Multimeter == null || ExecutionConfig.GetIsIdleModeEnabled())
    {
      return;
    }

    string mode = await context.Multimeter.DeviceProtocol.QueryAsync("FUNC?", timeout: MultimeterCommandTimeoutMs);
    LogInformation($"[Тест цифрового вольтметра] {operation}: режим после установки диапазона={mode}.", isDeviceLog: true);

    string error = await context.Multimeter.DeviceProtocol.QueryAsync("SYSTEM:ERROR?", timeout: MultimeterCommandTimeoutMs);
    LogInformation($"[Тест цифрового вольтметра] {operation}: ошибка после установки диапазона={error}.", isDeviceLog: true);
    if (!string.IsNullOrWhiteSpace(error) && !error.TrimStart().StartsWith("+0", StringComparison.Ordinal))
    {
      throw new InvalidOperationException($"Ошибка установки диапазона мультиметра: {error}");
    }
  }

  /// <summary>
  /// Возвращает команду измерения мультиметра с безопасной заменой пустого значения.
  /// </summary>
  private static string GetMeasureCommand(string? configuredCommand)
  {
    return string.IsNullOrWhiteSpace(configuredCommand)
      ? MultimeterMeasureCommand
      : configuredCommand;
  }

  /// <summary>
  /// Переводит мультиметр в режим немедленного запуска измерения.
  /// </summary>
  private static async Task SetImmediateTriggerAsync(LegacyAskSelfControlContext context, string operation)
  {
    if (context.Multimeter == null || ExecutionConfig.GetIsIdleModeEnabled())
    {
      return;
    }

    string response = await context.Multimeter.DeviceProtocol.QueryAsync("TRIG:SOUR IMM", timeout: MultimeterCommandTimeoutMs);
    LogInformation($"[Тест цифрового вольтметра] {operation}: команда TRIG:SOUR IMM, ответ={response}.", isDeviceLog: true);
  }

  /// <summary>
  /// Очищает очередь ошибок мультиметра перед настройкой очередного диапазона.
  /// </summary>
  private static async Task ClearMultimeterErrorsAsync(LegacyAskSelfControlContext context, string operation)
  {
    if (context.Multimeter == null || ExecutionConfig.GetIsIdleModeEnabled())
    {
      return;
    }

    string response = await context.Multimeter.DeviceProtocol.QueryAsync("*CLS", timeout: MultimeterCommandTimeoutMs);
    LogInformation($"[Тест цифрового вольтметра] {operation}: команда *CLS, ответ={response}.", isDeviceLog: true);
  }

  /// <summary>
  /// Определяет, относится ли ошибка измерения к ожидаемому признаку перегрузки мультиметра.
  /// </summary>
  private static bool IsExpectedOverloadException(Exception exception)
  {
    string message = exception.Message;
    return message.Contains("over", StringComparison.OrdinalIgnoreCase)
      || message.Contains("overflow", StringComparison.OrdinalIgnoreCase)
      || message.Contains("overload", StringComparison.OrdinalIgnoreCase)
      || message.Contains("перегр", StringComparison.OrdinalIgnoreCase)
      || message.Contains("OL", StringComparison.OrdinalIgnoreCase)
      || message.Contains("9.9E", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Пытается получить числовое значение из сырого ответа мультиметра.
  /// </summary>
  private static bool TryParseMeasurement(string response, out double value)
  {
    value = 0;
    string text = response.Trim().Replace("+", string.Empty);
    var match = Regex.Match(text, @"^[+-]?(?:\d+(?:[.,]\d*)?|[.,]\d+)(?:[eE][+-]?\d+)?");
    return match.Success
      && double.TryParse(match.Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
  }

  /// <summary>
  /// Подключает входы цифрового вольтметра к шинам через контроллер АСК.
  /// </summary>
  private static Task ConnectVoltmeterToBusAsync(
    LegacyAskControllerProtocol controller,
    ushort positiveBus,
    ushort negativeBus,
    bool isVoltageMode,
    CancellationToken cancellationToken)
  {
    ushort groundWord = isVoltageMode
      ? (ushort)(negativeBus | LegacyAskBus.GroundSource)
      : negativeBus;

    return ConnectVoltmeterToBusCoreAsync(controller, positiveBus, groundWord, cancellationToken);
  }

  /// <summary>
  /// Записывает оба подрегистра подключения вольтметра так же, как старый <c>V7wrbusrg</c>.
  /// </summary>
  private static async Task ConnectVoltmeterToBusCoreAsync(
    LegacyAskControllerProtocol controller,
    ushort inputBus,
    ushort groundBus,
    CancellationToken cancellationToken)
  {
    await controller.WriteSubRegisterAsync(LegacyAskRegister.V7Gate, 2, groundBus, cancellationToken);
    await controller.WriteSubRegisterAsync(LegacyAskRegister.V7Gate, 1, inputBus, cancellationToken);
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
  /// Включает или отключает реле КЗШ через регистр включения приборов.
  /// </summary>
  private static Task SetShortCircuitRelayAsync(
    LegacyAskControllerProtocol controller,
    bool isEnabled,
    CancellationToken cancellationToken)
  {
    return controller.WriteRegisterAsync(LegacyAskRegister.DevicePower, isEnabled ? ShortCircuitRelayBit : (ushort)0, cancellationToken);
  }

  /// <summary>
  /// Выбирает ближайший поддерживаемый диапазон мультиметра не ниже запрошенного.
  /// </summary>
  private static double ResolveSupportedRange(double requestedRange, double[] supportedRanges)
  {
    double requested = Math.Abs(requestedRange);
    if (supportedRanges.Length == 0)
    {
      return requested;
    }

    foreach (double supportedRange in supportedRanges.OrderBy(value => value))
    {
      if (requested <= supportedRange)
      {
        return supportedRange;
      }
    }

    return supportedRanges.Max();
  }

  /// <summary>
  /// Возвращает разрешение для SCPI-команды настройки диапазона напряжения.
  /// </summary>
  private static double ResolveVoltageResolution(double range)
  {
    return range switch
    {
      <= 0.1d => 0.0000001d,
      <= 1d => 0.000001d,
      <= 10d => 0.00001d,
      <= 100d => 0.0001d,
      _ => 0.001d
    };
  }

  /// <summary>
  /// Возвращает разрешение для SCPI-команды настройки диапазона сопротивления.
  /// </summary>
  private static double ResolveResistanceResolution(double range)
  {
    return range switch
    {
      <= 100.0 => 0.001,
      <= 1000.0 => 0.01,
      <= 10000.0 => 0.1,
      <= 100000.0 => 1.0,
      _ => 10.0
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
