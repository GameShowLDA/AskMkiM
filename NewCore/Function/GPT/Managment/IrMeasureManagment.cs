using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Device;
using NewCore.Function.GPT.Command;
using NewCore.Function.GPT.Helper;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;
using static NewCore.Function.GPT.Command.FunctionCommandManager;

namespace NewCore.Function.GPT.Managment
{
  /// <summary>
  /// Класс управления измерениями для режима IR (сопротивление изоляции).
  /// Использует специальный алгоритм с таймером и парсингом результата.
  /// </summary>
  public class IrMeasureManagment : IMeasurable
  {
    private readonly GPT79904 _gptModel;
    private readonly int _delayBeforeCall;
    private readonly Func<Task<double>> _getTestTime;
    private readonly Func<Task<double>> _getRampTime;
    private readonly Func<Task<bool>> _getIsIdleMode;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="IrMeasureManagment"/>.
    /// </summary>
    /// <param name="gptModel">Модель устройства GPT-79904.</param>
    /// <param name="delayBeforeCall">Задержка перед вызовом команды (мс).</param>
    /// <param name="getTestTime">Функция получения времени теста.</param>
    /// <param name="getRampTime">Функция получения времени нарастания.</param>
    /// <param name="getIsIdleMode">Функция для проверки Idle Mode устройства.</param>
    public IrMeasureManagment(
        GPT79904 gptModel,
        int delayBeforeCall,
        Func<Task<double>> getTestTime,
        Func<Task<double>> getRampTime,
        Func<Task<bool>> getIsIdleMode)
    {
      _gptModel = gptModel;
      _delayBeforeCall = delayBeforeCall;
      _getIsIdleMode = getIsIdleMode;
      _getTestTime = getTestTime;
      _getRampTime = getRampTime;
    }

    /// <inheritdoc />
    /// <summary>
    /// Выполняет измерение сопротивления изоляции.
    /// </summary>
    public async Task<(double value, string unit)> MeasureAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      bool waitFullTime = false,
      IUserInteractionService? userMessageService = null)
    {
      if (await _getIsIdleMode())
        return (param, string.Empty);

      await StopMeasure();
      await Task.Delay(_delayBeforeCall);

      var time = await _getTestTime();
      var timeRamp = await _getRampTime();

      int totalTicks = (int)((time + timeRamp) * 1000 / 200) - 1;
      var timer = new System.Timers.Timer
      {
        Interval = 200,
        AutoReset = true
      };

      int tickCount = 0;
      string response = string.Empty;
      var testCommand = $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
      MeasurementData? model = null;

      timer.Elapsed += async (s, a) =>
      {
        tickCount++;

        await Task.Delay(300);
        response = await _gptModel.DeviceProtocol.QueryAsync(
          $"{GetCommandSyntax(FunctionCommand.MEASURE)} ?",
          timeout: 500,
          delayBeforeCall: _delayBeforeCall);

        try
        {
          model = ParseMeasurement(response);
          if (model.Status.ToLower().Contains("fail"))
          {
            await _gptModel.DeviceProtocol.QueryAsync(testCommand);
          }
          else if (model.Status.ToLower().Contains("test") && model.Resistance > 0 && model.Resistance > param)
          {
            await StopMeasure();
            tickCount = totalTicks + 1;
            timer.Stop();
            return;
          }
        }
        catch
        {
          model = null;
        }
      };

      await _gptModel.DeviceProtocol.QueryAsync(testCommand);
      timer.Start();

      var task = Task.Run(async () =>
      {
        while (tickCount <= totalTicks)
          await Task.Delay(1);
      });

      Task.WaitAny(task);

      timer.Stop();
      timer.Dispose();

      while (true)
      {
        response = await _gptModel.DeviceProtocol.QueryAsync(
          $"{GetCommandSyntax(FunctionCommand.MEASURE)} ?",
          timeout: 500,
          delayBeforeCall: _delayBeforeCall);

        if (!response.ToLower().Contains("test"))
          break;

        await Task.Delay(50);
      }

      response = await _gptModel.DeviceProtocol.QueryAsync(
        $"{GetCommandSyntax(FunctionCommand.MEASURE)} ?",
        timeout: 500,
        delayBeforeCall: _delayBeforeCall);

      var parts = response.Split(',');
      string raw = parts.ElementAtOrDefault(3)?.ToLower()
        ?? throw new FormatException("Нет результата измерения.");

      double multiplier = raw.EndsWith("gohm") ? 1000 :
                          raw.EndsWith("mohm") ? 1 :
                          raw.EndsWith("kohm") ? 0.001 :
                          throw new FormatException("Неизвестный формат.");

      raw = Regex.Replace(raw, @"[^0-9.,]", "").Replace('.', ',');
      double value;

      while (!double.TryParse(raw, out value))
      {
        response = await _gptModel.DeviceProtocol.QueryAsync(
          $"{GetCommandSyntax(FunctionCommand.MEASURE)} ?",
          timeout: 500,
          delayBeforeCall: _delayBeforeCall);

        parts = response.Split(',');
        raw = parts.ElementAtOrDefault(3)?.ToLower()
          ?? throw new FormatException("Нет результата измерения.");

        multiplier = raw.EndsWith("gohm") ? 1000 :
                     raw.EndsWith("mohm") ? 1 :
                     raw.EndsWith("kohm") ? 0.001 :
                     throw new FormatException("Неизвестный формат.");

        raw = Regex.Replace(raw, @"[^0-9.,]", "").Replace('.', ',');
      }

      return (value * multiplier, string.Empty);
    }

    /// <inheritdoc />
    public async Task StopMeasure()
    {
      await MeasureHelper.StopMeasure(_gptModel);
    }

    /// <inheritdoc />
    public async Task ApplyVoltageAsync(IUserInteractionService? userMessageService = null)
    {
      LogInformation($"Начало {nameof(ApplyVoltageAsync)}", isDeviceLog: true);
      try
      {
        if (await _getIsIdleMode())
        {
          LogInformation($"{nameof(ApplyVoltageAsync)}: Устройство в Idle Mode. Пропускаем применение напряжения.", isDeviceLog: true);
          return;
        }

        var command = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
        await _gptModel.DeviceProtocol.QueryAsync(command, delayBeforeCall: _delayBeforeCall);
        LogInformation($"{nameof(ApplyVoltageAsync)}: Напряжение применено.", isDeviceLog: true);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(ApplyVoltageAsync)}", ex, isDeviceLog: true);
        throw;
      }
    }

    /// <summary>
    /// Разбирает строку ответа прибора в модель <see cref="MeasurementData"/>.
    /// </summary>
    /// <summary>
    /// Разбирает строку ответа прибора в модель MeasurementData.
    /// Ищет статус PASS / FAIL / TEST в любой части ответа.
    /// </summary>
    private MeasurementData ParseMeasurement(string response)
    {
      if (string.IsNullOrWhiteSpace(response))
        return new MeasurementData { Status = "UNKNOWN", Resistance = 0 };

      var parts = response.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(p => p.Trim())
                          .ToList();

      string status = "UNKNOWN";

      foreach (var p in parts)
      {
        var upper = p.ToUpperInvariant();

        if (upper.Contains("PASS"))
        {
          status = "PASS";
          break;
        }
        if (upper.Contains("FAIL"))
        {
          status = "FAIL";
          break;
        }
        if (upper.Contains("TEST"))
        {
          status = "TEST";
          break;
        }
      }

      double resistance = 0;

      foreach (var p in parts)
      {
        var match = Regex.Match(p, @"([-+]?\d+(\.\d+)?)([GMk]?)(ohm|OHM)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
          double value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
          string unit = match.Groups[3].Value.ToUpperInvariant();

          resistance = unit switch
          {
            "G" => value * 1_000_000,
            "M" => value * 1_000,
            _ => value
          };

          break;
        }
      }

      return new MeasurementData
      {
        Status = status,
        Resistance = resistance
      };
    }


    /// <summary>
    /// Модель для хранения результата измерения в режиме IR.
    /// </summary>
    public class MeasurementData
    {
      /// <summary>
      /// Статус, возвращённый устройством (например: TEST, FAIL, DONE).
      /// </summary>
      public string Status { get; set; } = string.Empty;

      /// <summary>
      /// Измеренное сопротивление в МОм (или в другой единице, в зависимости от парсинга).
      /// </summary>
      public double Resistance { get; set; }
    }
  }
}
