using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Function.GPT.Command;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;
using static NewCore.Function.GPT.Command.FunctionCommandManager;

namespace NewCore.Function.GPT.Helper
{
  static internal class MeasureHelper
  {
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

      LogInformation($"Начало {nameof(MeasureAsync)}", isDeviceLog: true);

      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        LogInformation($"{nameof(MeasureAsync)}: Устройство в Idle Mode. Возвращаем param.", isDeviceLog: true);
        return (param, "");
      }

      if (waitFullTime)
        return await MeasureFullTimeAsync(breakDown, delayBeforeCall);
      else
        return await MeasureFastPollingAsync(breakDown, time, delayBeforeCall);
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
      var count = (int)time;
      await TimeHelper.SetTestTimeAsync(breakDown, breakDown.Mode, 1, delayBeforeCall);
      string answerDevice = string.Empty;

      for (int i = 0; i < count; i++)
      {
        var query = $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
        await breakDown.DeviceProtocol.QueryAsync(query, delayBeforeCall: delayBeforeCall);

        while (true)
        {

          query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.MEASURE)} ?";
          answerDevice = await breakDown.DeviceProtocol.QueryAsync(query, responseDelay: 1000, timeout: 500, delayBeforeCall: delayBeforeCall);

          if (!answerDevice.Contains("TEST"))
            break;

          await Task.Delay(300);
        }

        if (!answerDevice.Contains("FAIL"))
        {
          break;
        }
      }

      await StopMeasure(breakDown);
      var (value, unit) = ParseMeasureValue(answerDevice);

      LogInformation($"[{nameof(MeasureFullTimeAsync)}] Значение = {value} {unit}", isDeviceLog: true);
      return (value, unit);
    }

    /// <summary>
    /// Полный режим: система полностью ждёт time + timeRamp и только после этого запрашивает результат измерения.
    /// </summary>
    static private async Task<(double value, string unit)> MeasureFullTimeAsync(
      IBreakdownTester breakDown,
      int delayBeforeCall)
    {
      LogInformation($"[{nameof(MeasureFullTimeAsync)}] Запуск полного измерения", isDeviceLog: true);

      var query = $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";

      await breakDown.DeviceProtocol.QueryAsync(query, delayBeforeCall: delayBeforeCall);
      string answerDevice = string.Empty;

      while (true)
      {
        await Task.Delay(100);

        query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.MEASURE)} ?";
        answerDevice = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 500, delayBeforeCall: delayBeforeCall);

        if (!answerDevice.Contains("TEST"))
          break;
      }

      var (value, unit) = ParseMeasureValue(answerDevice);

      LogInformation($"[{nameof(MeasureFullTimeAsync)}] Значение = {value} {unit}", isDeviceLog: true);
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
      string response = await breakDown.DeviceProtocol.QueryAsync($"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} OFF");
      await breakDown.DeviceProtocol.QueryAsync(response);
    }
  }
}
