using System.Globalization;
using System.Text.RegularExpressions;
using NewCore.Base.Function.Breakdown;
using NewCore.Device;
using NewCore.Function.GPT.Command;
using NewCore.Function.GPT.Data;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;
using static Utilities.LoggerUtility;
using static AppConfiguration.Execution.ExecutionConfig;

namespace NewCore.Function.GPT
{
  /// <summary>
  /// Класс для работы с режимом ACW (переменный ток высокого напряжения).
  /// </summary>
  public class AcwMode : IAcwModeBreakdown
  {
    /// <summary>
    /// Создает новый экземпляр класса <see cref="AcwMode"/>.
    /// </summary>
    /// <param name="gpt79904">Объект устройства GPT-79904.</param>
    public AcwMode(GPT79904 gpt79904) => _gptModel = gpt79904;

    /// <summary>
    /// Модель устройства GPT-79904.
    /// </summary>
    private GPT79904 _gptModel { get; set; }

    static private int delayBeforeCall = 100;

    int delay = 300;

    #region Mode

    /// <inheritdoc />
    /// <summary>
    /// Устанавливает режим ACW.
    /// </summary>
    public async Task<(bool Success, string Message)> SetModeAsync()
    {
      LogInformation($"Начало выполнения {nameof(SetModeAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetModeAsync)}: Устройство в Idle Mode. Пропускаем установку режима.");
          return (true, string.Empty);
        }

        string expectedMode = "ACW";
        string command = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} {expectedMode}";
        await _gptModel.DeviceProtocol.QueryAsync(command);

        await Task.Delay(delay);
        var actualMode = await GetModeAsync();
        if (actualMode.Equals(expectedMode, StringComparison.OrdinalIgnoreCase))
        {
          LogInformation($"{nameof(SetModeAsync)}: Режим успешно установлен с первой попытки.");
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetModeAsync)}: Повторная попытка установки режима ACW.");
        await _gptModel.DeviceProtocol.QueryAsync(command);
        actualMode = await GetModeAsync();
        if (actualMode.Equals(expectedMode, StringComparison.OrdinalIgnoreCase))
        {
          LogInformation($"{nameof(SetModeAsync)}: Режим успешно установлен со второй попытки.");
          return (true, string.Empty);
        }

        string error = $"Не удалось установить режим ACW. Устройство сообщает: {actualMode}";
        LogWarning(error);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetModeAsync)}", ex);
        throw;
      }
    }

    /// <inheritdoc />
    public async Task<string> GetModeAsync()
    {
      LogInformation($"Начало выполнения {nameof(GetModeAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetModeAsync)}: Устройство в Idle Mode. Возвращаем пустую строку.");
          return string.Empty;
        }

        await Task.Delay(delay);
        var query = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} ?";
        var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetModeAsync)}: \"{response}\"");

        var trimmed = response.Trim();
        LogInformation($"{nameof(GetModeAsync)}: Результат = {trimmed}");
        return trimmed;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetModeAsync)}", ex);
        throw;
      }
    }

    #endregion

    #region Voltage

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetVoltageAsync(double value)
    {
      LogInformation($"Начало {nameof(SetVoltageAsync)}: value={value:F3}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetVoltageAsync)}: Устройство в Idle Mode. Пропускаем установку.");
          return (true, string.Empty);
        }

        double kvValue = value/1000;
        string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_VOLTAGE)} {kvValue:F3}".Replace(',', '.');

        await _gptModel.DeviceProtocol.QueryAsync(command);
        await Task.Delay(delay);

        var actualKv = await GetVoltageAsync();
        if (Math.Abs(actualKv - kvValue) < 0.01)
        {
          LogInformation($"{nameof(SetVoltageAsync)}: Напряжение установлено успешно.");
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetVoltageAsync)}: Повторная попытка.");
        await _gptModel.DeviceProtocol.QueryAsync(command);
        actualKv = await GetVoltageAsync();
        if (Math.Abs(actualKv - kvValue) < 0.01)
        {
          LogInformation($"{nameof(SetVoltageAsync)}: Напряжение установлено успешно (со второй попытки).");
          return (true, string.Empty);
        }

        string error = $"Не удалось установить напряжение {kvValue:F3} кВ. Устройство сообщает: {actualKv:F3} кВ.";
        LogWarning(error);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetVoltageAsync)}", ex);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }

    /// <inheritdoc />
    public async Task<double> GetVoltageAsync()
    {
      LogInformation($"Начало {nameof(GetVoltageAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetVoltageAsync)}: Устройство в Idle Mode. Возвращаем 0.");
          return 0;
        }

        await Task.Delay(delay);
        var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_VOLTAGE)} ?";
        var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetVoltageAsync)}: \"{response}\"");

        if (double.TryParse(response.Replace("kV", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var voltage))
        {
          LogInformation($"{nameof(GetVoltageAsync)}: Результат = {voltage}");
          return voltage;
        }

        LogWarning($"{nameof(GetVoltageAsync)}: Не удалось разобрать напряжение. Возвращаем 0.");
        return 0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetVoltageAsync)}", ex);
        throw;
      }

    }

    #endregion

    #region HighCurrentLimit

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetHighCurrentLimitAsync(double value)
    {
      LogInformation($"Начало {nameof(SetHighCurrentLimitAsync)}: value={value:F3}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetHighCurrentLimitAsync)}: Устройство в Idle Mode. Пропускаем установку.");
          return (true, string.Empty);
        }

        string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CHISET)} {value:F3}".Replace(',', '.');
        await _gptModel.DeviceProtocol.QueryAsync(command);
        await Task.Delay(delay);

        var actual = await GetHighCurrentLimitAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetHighCurrentLimitAsync)}: Верхний предел тока успешно установлен.");
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetHighCurrentLimitAsync)}: Повторная попытка.");
        await _gptModel.DeviceProtocol.QueryAsync(command);
        actual = await GetHighCurrentLimitAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetHighCurrentLimitAsync)}: Верхний предел тока установлен со второй попытки.");
          return (true, string.Empty);
        }

        string error = $"Не удалось установить верхний предел тока {value:F3} мА. Устройство сообщает: {actual:F3} мА.";
        LogWarning(error);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetHighCurrentLimitAsync)}", ex);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }


    /// <inheritdoc />
    public async Task<double> GetHighCurrentLimitAsync()
    {
      LogInformation($"Начало {nameof(GetHighCurrentLimitAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetHighCurrentLimitAsync)}: Устройство в Idle Mode. Возвращаем 0.");
          return 0;
        }

        await Task.Delay(delay);
        var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CHISET)} ?";
        var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetHighCurrentLimitAsync)}: \"{response}\"");

        if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var current))
        {
          LogInformation($"{nameof(GetHighCurrentLimitAsync)}: Результат = {current}");
          return current;
        }

        LogWarning($"{nameof(GetHighCurrentLimitAsync)}: Не удалось разобрать ток. Возвращаем 0.");
        return 0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetHighCurrentLimitAsync)}", ex);
        throw;
      }
    }

    #endregion

    #region LowCurrentLimit

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetLowCurrentLimitAsync(double value)
    {
      LogInformation($"Начало {nameof(SetLowCurrentLimitAsync)}: value={value:F3}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetLowCurrentLimitAsync)}: Устройство в Idle Mode. Пропускаем установку.");
          return (true, string.Empty);
        }

        string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CLOSET)} {value:F3}".Replace(',', '.');
        await _gptModel.DeviceProtocol.QueryAsync(command);

        var actual = await GetLowCurrentLimitAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetLowCurrentLimitAsync)}: Нижний предел тока успешно установлен.");
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetLowCurrentLimitAsync)}: Повторная попытка.");
        await _gptModel.DeviceProtocol.QueryAsync(command);
        actual = await GetLowCurrentLimitAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetLowCurrentLimitAsync)}: Нижний предел тока установлен со второй попытки.");
          return (true, string.Empty);
        }

        string error = $"Не удалось установить нижний предел тока {value:F3} мА. Устройство сообщает: {actual:F3} мА.";
        LogWarning(error);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetLowCurrentLimitAsync)}", ex);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }


    /// <inheritdoc />
    public async Task<double> GetLowCurrentLimitAsync()
    {
      LogInformation($"Начало {nameof(GetLowCurrentLimitAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetLowCurrentLimitAsync)}: Устройство в Idle Mode. Возвращаем 0.");
          return 0;
        }

        await Task.Delay(delay);
        var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CLOSET)} ?";
        var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetLowCurrentLimitAsync)}: \"{response}\"");

        if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var current))
        {
          LogInformation($"{nameof(GetLowCurrentLimitAsync)}: Результат = {current}");
          return current;
        }

        LogWarning($"{nameof(GetLowCurrentLimitAsync)}: Не удалось разобрать ток. Возвращаем 0.");
        return 0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetLowCurrentLimitAsync)}", ex);
        throw;
      }
    }
    #endregion

    #region TestTime

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetTestTimeAsync(double value)
    {
      LogInformation($"Начало {nameof(SetTestTimeAsync)}: value={value:F1}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetTestTimeAsync)}: Устройство в Idle Mode. Пропускаем установку.");
          return (true, string.Empty);
        }

        await Task.Delay(delay);
        string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_TTIME)} {value:F1}".Replace(',', '.');

        await _gptModel.DeviceProtocol.QueryAsync(command);
        var actual = await GetTestTimeAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetTestTimeAsync)}: Время теста успешно установлено.");
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetTestTimeAsync)}: Повторная попытка.");
        await _gptModel.DeviceProtocol.QueryAsync(command);
        actual = await GetTestTimeAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetTestTimeAsync)}: Время теста установлено со второй попытки.");
          return (true, string.Empty);
        }

        string error = $"Не удалось установить время теста {value:F1} сек. Устройство сообщает: {actual:F1} сек.";
        LogWarning(error);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetTestTimeAsync)}", ex);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }

    /// <inheritdoc />
    public async Task<double> GetTestTimeAsync()
    {
      LogInformation($"Начало {nameof(GetTestTimeAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetTestTimeAsync)}: Устройство в Idle Mode. Возвращаем 0.");
          return 0;
        }

        var query = GetCommandSyntax(ManualCommand.MANU_ACW_TTIME) + "?";
        var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetTestTimeAsync)}: \"{response}\"");

        var match = Regex.Match(response, @"\d+(\.\d+)?");
        if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var testTime))
        {
          LogInformation($"{nameof(GetTestTimeAsync)}: Результат = {testTime}");
          return testTime;
        }

        LogWarning($"{nameof(GetTestTimeAsync)}: Не удалось разобрать время. Возвращаем 0.");
        return 0.0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetTestTimeAsync)}", ex);
        throw;
      }
    }

    #endregion

    #region RampTime

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetRampTimeAsync(double value)
    {
      LogInformation($"Начало {nameof(SetRampTimeAsync)}: value={value:F1}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetRampTimeAsync)}: Устройство в Idle Mode. Пропускаем установку.");
          return (true, string.Empty);
        }

        await Task.Delay(delay);
        string command = $"{GetCommandSyntax(ManualCommand.MANU_RTIME)} {value:F1}".Replace(',', '.');

        await _gptModel.DeviceProtocol.QueryAsync(command);
        var actual = await GetRampTimeAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetRampTimeAsync)}: Ramp Time успешно установлен.");
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetRampTimeAsync)}: Повторная попытка.");
        await _gptModel.DeviceProtocol.QueryAsync(command);
        actual = await GetRampTimeAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetRampTimeAsync)}: Ramp Time установлен со второй попытки.");
          return (true, string.Empty);
        }

        string error = $"Не удалось установить Ramp Time {value:F1} сек. Устройство сообщает: {actual:F1} сек.";
        LogWarning(error);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetRampTimeAsync)}", ex);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }

    /// <inheritdoc />
    public async Task<double> GetRampTimeAsync()
    {
      LogInformation($"Начало {nameof(GetRampTimeAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetRampTimeAsync)}: Устройство в Idle Mode. Возвращаем 0.");
          return 0;
        }

        var query = GetCommandSyntax(ManualCommand.MANU_RTIME) + "?";
        var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetRampTimeAsync)}: \"{response}\"");

        var match = Regex.Match(response, @"\d+(\.\d+)?");
        if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rampTime))
        {
          LogInformation($"{nameof(GetRampTimeAsync)}: Результат = {rampTime}");
          return rampTime;
        }

        LogWarning($"{nameof(GetRampTimeAsync)}: Не удалось разобрать Ramp Time. Возвращаем 0.");
        return 0.0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetRampTimeAsync)}", ex);
        throw;
      }
    }

    #endregion

    #region Frequency

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetFrequencyAsync(int frequency)
    {
      LogInformation($"Начало {nameof(SetFrequencyAsync)}: frequency={frequency}");

      try
      {
        if (frequency != 50 && frequency != 60)
          throw new ArgumentException("Частота должна быть 50 или 60 Гц.");

        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetFrequencyAsync)}: Устройство в Idle Mode. Пропускаем установку.");
          return (true, string.Empty);
        }

        await Task.Delay(delay);
        string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_FREQUENCY)} {frequency}";

        await _gptModel.DeviceProtocol.QueryAsync(command);
        var actual = await GetFrequencyAsync();
        if (actual == frequency)
        {
          LogInformation($"{nameof(SetFrequencyAsync)}: Частота успешно установлена.");
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetFrequencyAsync)}: Повторная попытка.");
        await _gptModel.DeviceProtocol.QueryAsync(command);
        actual = await GetFrequencyAsync();
        if (actual == frequency)
        {
          LogInformation($"{nameof(SetFrequencyAsync)}: Частота успешно установлена со второй попытки.");
          return (true, string.Empty);
        }

        string error = $"Не удалось установить частоту {frequency} Гц. Устройство сообщает: {actual} Гц.";
        LogWarning(error);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetFrequencyAsync)}", ex);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }

    /// <inheritdoc />
    public async Task<int> GetFrequencyAsync()
    {
      LogInformation($"Начало {nameof(GetFrequencyAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetFrequencyAsync)}: Устройство в Idle Mode. Возвращаем 0.");
          return 0;
        }

        var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_FREQUENCY)} ?";
        var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetFrequencyAsync)}: \"{response}\"");

        if (int.TryParse(response.Replace("Hz", "").Trim(), out var freq))
        {
          LogInformation($"{nameof(GetFrequencyAsync)}: Результат = {freq}");
          return freq;
        }

        LogWarning($"{nameof(GetFrequencyAsync)}: Не удалось разобрать частоту. Возвращаем 0.");
        return 0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetFrequencyAsync)}", ex);
        throw;
      }
    }

    #endregion

    #region Offset

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetOffsetAsync(double value)
    {
      LogInformation($"Начало {nameof(SetOffsetAsync)}: value={value:F3}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetOffsetAsync)}: Устройство в Idle Mode. Пропускаем установку.");
          return (true, string.Empty);
        }

        string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_REF)} {value:F3}".Replace(',', '.');
        await _gptModel.DeviceProtocol.QueryAsync(command);

        var actual = await GetOffsetAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetOffsetAsync)}: Смещение успешно установлено.");
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetOffsetAsync)}: Повторная попытка.");
        await _gptModel.DeviceProtocol.QueryAsync(command);
        actual = await GetOffsetAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetOffsetAsync)}: Смещение установлено со второй попытки.");
          return (true, string.Empty);
        }

        string error = $"Не удалось установить смещение {value:F3} мА. Устройство сообщает: {actual:F3} мА.";
        LogWarning(error);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetOffsetAsync)}", ex);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }

    /// <inheritdoc />
    public async Task<double> GetOffsetAsync()
    {
      LogInformation($"Начало {nameof(GetOffsetAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetOffsetAsync)}: Устройство в Idle Mode. Возвращаем 0.");
          return 0;
        }

        var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_REF)} ?";
        var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetOffsetAsync)}: \"{response}\"");

        if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var offset))
        {
          LogInformation($"{nameof(GetOffsetAsync)}: Результат = {offset}");
          return offset;
        }

        LogWarning($"{nameof(GetOffsetAsync)}: Не удалось разобрать смещение. Возвращаем 0.");
        return 0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetOffsetAsync)}", ex);
        throw;
      }
    }

    #endregion

    #region ArcCurrent

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetArcCurrentAsync(double value)
    {
      LogInformation($"Начало {nameof(SetArcCurrentAsync)}: value={value:F3}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetArcCurrentAsync)}: Устройство в Idle Mode. Пропускаем установку.");
          return (true, string.Empty);
        }

        string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_ARCCURRENT)} {value:F3}".Replace(',', '.');
        await _gptModel.DeviceProtocol.QueryAsync(command);

        var actual = await GetArcCurrentAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetArcCurrentAsync)}: Дуговой ток успешно установлен.");
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetArcCurrentAsync)}: Повторная попытка.");
        await _gptModel.DeviceProtocol.QueryAsync(command);
        actual = await GetArcCurrentAsync();
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetArcCurrentAsync)}: Дуговой ток установлен со второй попытки.");
          return (true, string.Empty);
        }

        string error = $"Не удалось установить ток дуги {value:F3} мА. Устройство сообщает: {actual:F3} мА.";
        LogWarning(error);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetArcCurrentAsync)}", ex);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }

    /// <inheritdoc />
    public async Task<double> GetArcCurrentAsync()
    {
      LogInformation($"Начало {nameof(GetArcCurrentAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetArcCurrentAsync)}: Устройство в Idle Mode. Возвращаем 0.");
          return 0;
        }

        var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_ARCCURRENT)} ?";
        var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetArcCurrentAsync)}: \"{response}\"");

        if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var arc))
        {
          LogInformation($"{nameof(GetArcCurrentAsync)}: Результат = {arc}");
          return arc;
        }

        LogWarning($"{nameof(GetArcCurrentAsync)}: Не удалось разобрать ток дуги. Возвращаем 0.");
        return 0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetArcCurrentAsync)}", ex);
        throw;
      }
    }

    #endregion

    /// <summary>
    /// Считывает текущую конфигурацию ACW.
    /// </summary>
    public async Task<AcwConfiguration> ReadConfigurationAsync()
    {
      LogInformation($"Начало {nameof(ReadConfigurationAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(ReadConfigurationAsync)}: Устройство в Idle Mode. Возвращаем пустую конфигурацию.");
          return new AcwConfiguration();
        }

        double voltage = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_VOLTAGE, "kV");
        double highCurrentLimit = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_CHISET, "mA");
        double lowCurrentLimit = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_CLOSET, "mA");
        double testTime = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_TTIME, "S");
        int frequency = await ReadIntParameterAsync(ManualCommand.MANU_ACW_FREQUENCY, "Hz");
        double offset = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_REF, "mA");
        double arcCurrent = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_ARCCURRENT, "mA");

        var config = new AcwConfiguration
        {
          Voltage = voltage,
          HighCurrentLimit = highCurrentLimit,
          LowCurrentLimit = lowCurrentLimit,
          TestTime = testTime,
          Frequency = frequency,
          Offset = offset,
          ArcCurrent = arcCurrent,
        };

        LogInformation($"{nameof(ReadConfigurationAsync)}: Конфигурация успешно считана.");
        return config;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(ReadConfigurationAsync)}", ex);
        throw;
      }
    }

    /// <inheritdoc />
    /// <summary>
    /// Выполняет измерение тока ACW.
    /// </summary>
    public async Task<double> MeasureCurrentAsync(double param = 0)
    {
      LogInformation($"Начало {nameof(MeasureCurrentAsync)}");

      try
      {
        if (await GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(MeasureCurrentAsync)}: Устройство в Idle Mode. Возвращаем param.");
          return param;
        }

        var query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
        var timeDelay = Convert.ToInt32(await GetRampTimeAsync() + await GetTestTimeAsync());

        await _gptModel.DeviceProtocol.QueryAsync(query, responseDelay: timeDelay * 1000, delayBeforeCall: delayBeforeCall);
        query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.MEASURE)} ?";
        var answerDevice = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 500, delayBeforeCall: delayBeforeCall);

        var result = answerDevice.Split(',');
        var measureResulte = result[3];

        LogInformation($"Результат измерения режима ACW: {measureResulte}");

        Match match = Regex.Match(measureResulte, @"\d+(\.\d+)?");
        if (match.Success)
        {
          var finalResult = double.Parse(match.Value, CultureInfo.InvariantCulture);
          LogInformation($"{nameof(MeasureCurrentAsync)}: Возвращаем значение = {finalResult}");
          return finalResult;
        }

        throw new FormatException("Число не найдено в строке.");
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(MeasureCurrentAsync)}", ex);
        throw;
      }
    }

    /// <summary>
    /// Считывает числовой параметр из устройства.
    /// </summary>
    /// <param name="command">Команда запроса.</param>
    /// <param name="unit">Единица измерения.</param>
    /// <returns>Извлеченное значение.</returns>
    private async Task<double> ReadDoubleParameterAsync(ManualCommand command, string unit)
    {
      if (await GetIsIdleModeEnabled())
      {
        return 0;
      }

      var query = $"{GetCommandSyntax(command)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, 100, 1000);
      return double.Parse(response.Replace(unit, "").Trim().Replace(".", ","));
    }

    /// <summary>
    /// Считывает целочисленный параметр из устройства.
    /// </summary>
    /// <param name="command">Команда запроса.</param>
    /// <param name="unit">Единица измерения.</param>
    /// <returns>Извлеченное значение.</returns>
    private async Task<int> ReadIntParameterAsync(ManualCommand command, string unit)
    {
      if (await GetIsIdleModeEnabled())
      {
        return 0;
      }

      var query = $"{GetCommandSyntax(command)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, 100);
      return int.Parse(response.Replace(unit, "").Trim());
    }
  }
}
