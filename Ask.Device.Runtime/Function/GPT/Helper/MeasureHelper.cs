using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.GPT.Command;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;
using static Ask.Device.Runtime.Function.GPT.Command.FunctionCommandManager;

namespace Ask.Device.Runtime.Function.GPT.Helper
{
  static internal class MeasureHelper
  {
    private const int PollIntervalMs = 100;
    private const int StopPollIntervalMs = 50;

    /// <summary>
    /// Выполняет измерение.
    /// </summary>
    static public async Task<(double value, string unit)> MeasureAsync(
      IBreakdownTester breakDown,
      double time,
      double timeRamp,
      int delayBeforeCall,
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      bool waitFullTime = false,
      IUserInteractionService? userMessageService = null)
    {
      if (time == 60)
      {
        waitFullTime = true;
      }

      var total = Stopwatch.StartNew();
      LogInformation($"[PERF][GPT][Measure] Start: time={time}, ramp={timeRamp}, waitFullTime={waitFullTime}, delayBeforeCall={delayBeforeCall}", isDeviceLog: true);

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        LogInformation($"{nameof(MeasureAsync)}: Устройство в Idle Mode. Возвращаем param.", isDeviceLog: true);
        return (param, "");
      }

      try
      {
        if (waitFullTime)
          return await MeasureFullTimeAsync(breakDown, delayBeforeCall);
        else
          return await MeasureFastPollingAsync(breakDown, time, delayBeforeCall);
      }
      finally
      {
        total.Stop();
        LogInformation($"[PERF][GPT][Measure] Total: {total.ElapsedMilliseconds} ms", isDeviceLog: true);
      }
    }

    /// <summary>
    /// Быстрый режим: циклический опрос MEASURE без полного ожидания времени измерения.
    /// - PASS  → завершаем немедленно — измерение успешно
    /// - TEST  → тоже завершаем — устройство завершило измерение, но ещё не выдало PASS/FAIL
    /// - FAIL  → перезапускаем измерение
    /// - Unknown → продолжаем цикл
    /// </summary>
    static private async Task<(double value, string unit)> MeasureFastPollingAsync(
      IBreakdownTester breakDown,
      double time,
      int delayBeforeCall)
    {
      var total = Stopwatch.StartNew();
      var count = (int)time;
      var stage = Stopwatch.StartNew();
      LogInformation($"[PERF][GPT][MeasureFastPolling] Use configured test time: {stage.ElapsedMilliseconds} ms", isDeviceLog: true);
      string answerDevice = string.Empty;

      for (int i = 0; i < count; i++)
      {
        var query = $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
        stage.Restart();
        await breakDown.DeviceProtocol.QueryAsync(query, delayBeforeCall: delayBeforeCall);
        LogInformation($"[PERF][GPT][MeasureFastPolling] Start test #{i + 1}: {stage.ElapsedMilliseconds} ms", isDeviceLog: true);

        var poll = Stopwatch.StartNew();
        while (true)
        {

          query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.MEASURE)} ?";
          answerDevice = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 500, delayBeforeCall: delayBeforeCall);

          if (answerDevice != string.Empty && !answerDevice.Contains("TEST"))
            break;

          await Task.Delay(PollIntervalMs);
        }
        LogInformation($"[PERF][GPT][MeasureFastPolling] Poll result #{i + 1}: {poll.ElapsedMilliseconds} ms", isDeviceLog: true);

        if (!answerDevice.Contains("FAIL"))
        {
          break;
        }
      }

      stage.Restart();
      await StopMeasure(breakDown);
      LogInformation($"[PERF][GPT][MeasureFastPolling] Stop test: {stage.ElapsedMilliseconds} ms", isDeviceLog: true);
      var (value, unit) = ParseMeasureValue(answerDevice);

      if (breakDown.Mode != Ask.Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.IR)
      {
        if (answerDevice.Contains("FAIL"))
        {
          value = -1;
        }
      }

      LogInformation($"[PERF][GPT][MeasureFastPolling] Total: {total.ElapsedMilliseconds} ms; value={value} {unit}", isDeviceLog: true);
      return (value, unit);
    }

    /// <summary>
    /// Полный режим: система полностью ждёт time + timeRamp и только после этого запрашивает результат измерения.
    /// </summary>
    static private async Task<(double value, string unit)> MeasureFullTimeAsync(
      IBreakdownTester breakDown,
      int delayBeforeCall)
    {
      var total = Stopwatch.StartNew();
      LogInformation($"[{nameof(MeasureFullTimeAsync)}] Запуск полного измерения", isDeviceLog: true);

      var query = $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";

      var stage = Stopwatch.StartNew();
      await breakDown.DeviceProtocol.QueryAsync(query, delayBeforeCall: delayBeforeCall);
      LogInformation($"[PERF][GPT][MeasureFullTime] Start test: {stage.ElapsedMilliseconds} ms", isDeviceLog: true);
      string answerDevice = string.Empty;

      var poll = Stopwatch.StartNew();
      while (true)
      {
        await Task.Delay(PollIntervalMs);

        query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.MEASURE)} ?";
        answerDevice = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 500, delayBeforeCall: delayBeforeCall);

        if (!string.IsNullOrEmpty(answerDevice) && !answerDevice.Contains("TEST"))
          break;
      }
      LogInformation($"[PERF][GPT][MeasureFullTime] Poll result: {poll.ElapsedMilliseconds} ms", isDeviceLog: true);

      var (value, unit) = ParseMeasureValue(answerDevice);

      LogInformation($"[PERF][GPT][MeasureFullTime] Total: {total.ElapsedMilliseconds} ms; value={value} {unit}", isDeviceLog: true);
      return (value, unit);
    }

    /// <summary>
    /// Парсит строку ответа MEASURE и извлекает значение и единицу измерения.
    /// </summary>
    static private (double value, string unit) ParseMeasureValue(string answer)
    {
      var parts = answer.Split(',');
      if (parts.Length < 4)
        throw new FormatException("Некорректный формат ответа прибора.");

      var source = parts[3].Trim();
      LogInformation($"Парсинг измерения: {source}", isDeviceLog: true);

      // 1) Старый регекс (без пробела между числом и единицей)
      var match = Regex.Match(source, @"(?<value>\d+(\.\d+)?)(?<unit>[A-Za-z]+)");

      if (!match.Success)
      {
        // 2) Новая версия (разрешаем пробелы)
        match = Regex.Match(source, @"(?<value>\d+(?:\.\d+)?)[\s]*(?<unit>[A-Za-z]+)");
      }

      if (!match.Success)
        throw new FormatException($"Не удалось выделить число и единицу измерения из '{source}'.");

      double value = double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
      string unit = match.Groups["unit"].Value;

      return (value, unit);
    }

    /// <summary>
    /// Останавливает текущее измерение.
    /// </summary>
    static public async Task StopMeasure(IBreakdownTester breakDown)
    {
      var total = Stopwatch.StartNew();
      var stopCommand = $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} OFF";
      var statusCommand = $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ?";

      while (true)
      {
        await breakDown.DeviceProtocol.QueryAsync(stopCommand);
        await Task.Delay(StopPollIntervalMs);

        var answerDevice = await breakDown.DeviceProtocol.QueryAsync(statusCommand, responseDelay: StopPollIntervalMs, timeout: 1000);

        if (!string.IsNullOrWhiteSpace(answerDevice)
          && answerDevice.Contains("TEST OFF", StringComparison.OrdinalIgnoreCase))
        {
          LogInformation($"[PERF][GPT][StopMeasure] Total: {total.ElapsedMilliseconds} ms", isDeviceLog: true);
          return;
        }

        await Task.Delay(StopPollIntervalMs);
      }
    }
  }
}
