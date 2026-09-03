using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing;
using Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common;
using Ask.Device.Runtime.Function.GPT.Command;
using System.Diagnostics;
using static Ask.Device.Runtime.Function.GPT.Command.FunctionCommandManager;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.GPT.Helper
{
  static internal class MeasureHelper
  {
    private const int PollIntervalMs = 100;
    private const int StopPollIntervalMs = 50;

    /// <summary>
    /// Выполняет измерение.
    /// </summary>
    static public async Task<BreakdownMeasurementResponse> MeasureAsync(
      IBreakdownTester breakDown,
      double time,
      double timeRamp,
      int delayBeforeCall,
      ElectricalTestFunction electricalTestFunction,
      MeasurementRange measurementRange,
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
        var random = Simulated.GetSimulatedValue(measurementRange.LowerBound, measurementRange.UpperBound, electricalTestFunction);
        await breakDown.DeviceProtocol.QueryAsync(
          $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON",
          delayBeforeCall: delayBeforeCall);

        await breakDown.DeviceProtocol.QueryAsync(
          $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.MEASURE)} ?",
          timeout: 500,
          delayBeforeCall: delayBeforeCall);

        await breakDown.DeviceProtocol.QueryAsync($"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} OFF");
        await breakDown.DeviceProtocol.QueryAsync(
          $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ?",
          responseDelay: StopPollIntervalMs,
          timeout: 1000);

        LogInformation($"{nameof(MeasureAsync)}: Устройство в Idle Mode. Возвращаем {random}.", isDeviceLog: true);
        return new BreakdownMeasurementResponse(BreakdownMeasurementStatus.Pass, random, string.Empty);
      }

      try
      {
        if (!waitFullTime)
        {
          await breakDown.Time.SetTestTimeAsync(1);
          var answer = await MeasureFullTimeAsync(breakDown, delayBeforeCall);
          if (answer.Status == BreakdownMeasurementStatus.Pass)
          {
            return answer;
          }

          await breakDown.LimitManager.SetLowLimitAsync(1);
        }

        await breakDown.Time.SetTestTimeAsync(breakDown.Time.GetTargetTime());
        return await MeasureFullTimeAsync(breakDown, delayBeforeCall);
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
    static private async Task<BreakdownMeasurementResponse> MeasureFastPollingAsync(
      IBreakdownTester breakDown,
      double time,
      int delayBeforeCall)
    {
      var total = Stopwatch.StartNew();
      var count = (int)time;
      var stage = Stopwatch.StartNew();
      LogInformation($"[PERF][GPT][MeasureFastPolling] Use configured test time: {stage.ElapsedMilliseconds} ms", isDeviceLog: true);
      string answerDevice = string.Empty;
      var attempt = 0;

      do
      {
        attempt++;
        stage.Restart();
        var query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.MEASURE)} ?";

        await breakDown.DeviceProtocol.QueryAsync(query, delayBeforeCall: delayBeforeCall);
        LogInformation($"[PERF][GPT][MeasureFastPolling] Start test #{attempt}: {stage.ElapsedMilliseconds} ms", isDeviceLog: true);

        var poll = Stopwatch.StartNew();
        while (true)
        {
          answerDevice = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 500, delayBeforeCall: delayBeforeCall);

          if (answerDevice != string.Empty
            && !BreakdownTesterResponseProcessor.IsTestInProgress(answerDevice))
            break;

          await Task.Delay(PollIntervalMs);
        }
        LogInformation($"[PERF][GPT][MeasureFastPolling] Poll result #{attempt}: {poll.ElapsedMilliseconds} ms", isDeviceLog: true);

        if (!BreakdownTesterResponseProcessor.IsTestFailed(answerDevice))
        {
          break;
        }
      }
      while (total.Elapsed.TotalSeconds < time);

      stage.Restart();
      await StopMeasure(breakDown);
      LogInformation($"[PERF][GPT][MeasureFastPolling] Stop test: {stage.ElapsedMilliseconds} ms", isDeviceLog: true);
      var answer = ParseMeasureValue(answerDevice);

      if (breakDown.Mode != BreakdownTypeMode.IR)
      {
        if (BreakdownTesterResponseProcessor.IsTestFailed(answerDevice))
        {
          answer.Value = -1;
        }
      }

      LogInformation($"[PERF][GPT][MeasureFastPolling] Total: {total.ElapsedMilliseconds} ms; value={answer.Value} {answer.Unit}", isDeviceLog: true);
      return answer;
    }

    /// <summary>
    /// Выполняет измерение с полным ожиданием времени тестирования и времени нарастания,
    /// после чего запрашивает результат измерения.
    /// </summary>
    /// <param name="breakDown">Устройство для проведения испытания на пробой.</param>
    /// <param name="delayBeforeCall">Задержка перед выполнением команды устройства.</param>
    /// <returns>Результат измерения после завершения испытания.</returns>
    static private async Task<BreakdownMeasurementResponse> MeasureFullTimeAsync(
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

        if (!string.IsNullOrEmpty(answerDevice)
          && !BreakdownTesterResponseProcessor.IsTestInProgress(answerDevice))
          break;
      }
      LogInformation($"[PERF][GPT][MeasureFullTime] Poll result: {poll.ElapsedMilliseconds} ms", isDeviceLog: true);

      var answer = ParseMeasureValue(answerDevice);

      LogInformation($"[PERF][GPT][MeasureFullTime] Total: {total.ElapsedMilliseconds} ms; value={answer.Value} {answer.Unit}", isDeviceLog: true);
      return answer;
    }

    /// <summary>
    /// Парсит строку ответа MEASURE и извлекает значение и единицу измерения.
    /// </summary>
    static private BreakdownMeasurementResponse ParseMeasureValue(string answer)
    {
      if (!BreakdownTesterResponseProcessor.TryParseMeasurement(answer, out var response))
        throw new FormatException("Некорректный формат ответа прибора.");

      LogInformation($"Парсинг измерения: {response.Value} {response.Unit}", isDeviceLog: true);
      return response;
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
          && BreakdownTesterResponseProcessor.IsTestStopped(answerDevice))
        {
          LogInformation($"[PERF][GPT][StopMeasure] Total: {total.ElapsedMilliseconds} ms", isDeviceLog: true);
          return;
        }

        await Task.Delay(StopPollIntervalMs);
      }
    }
  }
}
