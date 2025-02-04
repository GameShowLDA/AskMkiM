using System.Configuration;
using Core.GptLibrary.Command;
using Core.GptLibrary.Data;
using static Core.GptLibrary.Command.FunctionCommandManager;
using static Core.GptLibrary.Command.ManualCommandManager;
using static Utilities.LoggerUtility;

namespace Core.GptLibrary
{
  /// <summary>
  /// Функции режима "Сопротивление изоляции".
  /// </summary>
  public static class IrMode
  {

    static private int timeDelay = 2;

    /// <summary>
    /// Устанавливает режим сопротивления изоляции на пробойке.
    /// </summary>
    /// <param name="model">Модель пробойки.</param>
    static public async Task SetModeAsync(GptLibrary.Model model)
    {
      LogInformation("Устанавливаем режим СИ на GPT-79904");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} IR";
      await model.WriteLineAsync(query);
    }


    /// <summary>
    /// Устанавливает напряжения на пробойном устройстве.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение.</param>
    static public async Task SetVoltageAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем напряжение {value} для режима СИ на GPT-79904");
      string valueResult = (value / 1000).ToString().Replace(",", ".");
      var query = $"{ManualCommandManager.GetCommandSyntax(ManualCommand.MANU_IR_VOLTAGE)} {valueResult}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Установка/возврат времени теста в секундах.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение.</param>
    /// <returns></returns>
    static public async Task SetTimeAsync(Model model, int value)
    {
      LogInformation($"Устанавливаем время измерения {value} для режима СИ на GPT-79904");
      var query = $"{ManualCommandManager.GetCommandSyntax(ManualCommand.MANU_IR_TTIME)} {value}";
      await model.WriteLineAsync(query);
      timeDelay = value;
    }

    /// <summary>
    /// Возвращает напряжение на пробойном устройстве.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <returns></returns>
    static public async Task<double> GetVoltageAsync(Model model)
    {
      LogInformation("Считывание данных с ПУ");
      var query = $"{ManualCommandManager.GetCommandSyntax(ManualCommand.MANU_IR_VOLTAGE)} ?";
      await model.WriteLineAsync(query);

      var return_value = await model.ReadLineAsync();
      string numericPart = return_value.Replace("kV", string.Empty).Trim().Replace(".", ",");

      if (double.TryParse(numericPart, out double voltageInVolts))
      {
        double voltageInMillivolts = voltageInVolts * 1000;
        LogInformation($"Напряжение при режиме СИ: {voltageInMillivolts} В");
        return voltageInMillivolts;
      }
      else
      {
        LogError("Ошибка чтения напряжение в режиме СИ: не удалось преобразовать строку в число.");
        return -1.0;
      }
    }

    /// <summary>
    /// Измерение сопротивления с преобразованием результата в МОм.
    /// </summary>
    /// <param name="model"></param>
    /// <returns>Результат измерения в МОм.</returns>
    static public async Task<double> MeasureResistanceAsync(Model model)
    {
      LogInformation("Запуск измерений режима СИ");
      var query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
      await model.WriteLineAsync(query);
      await Task.Delay((timeDelay + 2) * 1000);

      query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.MEASURE)} ?";
      await model.WriteLineAsync(query);
      await Task.Delay(500);

      var answerDevice = await model.ReadLineAsync();
      var result = answerDevice.Split(',');
      var measureResulte = result[3];

      LogInformation($"Результат измерения режима СИ: {measureResulte}");

      double multiplier = 1.0;
      string numericPart = measureResulte.ToLower();

      if (numericPart.EndsWith("gohm"))
      {
        multiplier = 1_000; // ГОм -> МОм
        numericPart = numericPart.Replace("gohm", "").Trim();
      }
      else if (numericPart.EndsWith("mohm"))
      {
        multiplier = 1; // МОм -> МОм
        numericPart = numericPart.Replace("mohm", "").Trim();
      }
      else if (numericPart.EndsWith("kohm"))
      {
        multiplier = 0.001; // кОм -> МОм
        numericPart = numericPart.Replace("kohm", "").Trim();
      }
      else
      {
        LogError($"Неизвестный формат результата: {measureResulte}");
        throw new FormatException("Неподдерживаемый формат результата измерения.");
      }

      numericPart = numericPart.Replace('.', ',');

      // Преобразование числовой части
      if (double.TryParse(numericPart, out var resistanceValue))
      {
        var resistanceInMegaOhms = resistanceValue * multiplier;
        LogInformation($"Перевод в МОм: {resistanceInMegaOhms}");
        return resistanceInMegaOhms;
      }
      else
      {
        LogError($"Ошибка преобразования значения: {measureResulte}");
        throw new FormatException("Не удалось преобразовать значение сопротивления.");
      }
    }

    /// <summary>
    /// Возвращает список напряжений для заданного сопротивления.
    /// </summary>
    /// <param name="resistance">Сопротивление в МОм.</param>
    /// <returns>Список напряжений.</returns>
    public static List<int> GetVoltagesForResistance(double resistance)
    {
      var voltages = new List<int>();

      if (resistance >= 0.1 && resistance <= 0.3)
      {
        voltages.Add(50);
        voltages.Add(100);
      }
      else if (resistance > 0.3 && resistance <= 1.0)
      {
        voltages.Add(100);
        voltages.Add(200);
      }
      else if (resistance > 1.0 && resistance <= 3.0)
      {
        voltages.Add(200);
        voltages.Add(500);
      }
      else if (resistance > 3.0 && resistance <= 10.0)
      {
        voltages.Add(200);
        voltages.Add(500);
      }
      else if (resistance > 10.0 && resistance <= 30.0)
      {
        voltages.Add(200);
        voltages.Add(500);
      }
      else if (resistance > 30.0 && resistance <= 100.0)
      {
        voltages.Add(200);
        voltages.Add(500);
      }
      else if (resistance > 100.0 && resistance <= 300.0)
      {
        voltages.Add(200);
        voltages.Add(500);
      }
      else if (resistance > 300.0 && resistance <= 1000.0)
      {
        voltages.Add(200);
        voltages.Add(500);
      }
      else
      {
        LogError("Сопротивление вне поддерживаемого диапазона.");
      }

      return voltages;
    }

    /// <summary>
    /// Устанавливает высокий предел сопротивления IR.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в ГОм).</param>
    public static async Task SetHighResistanceLimitAsync(Model model, double value)
    {
      var query = $"{GetCommandSyntax(ManualCommand.MANU_IR_RHISET)} {value:F3}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает низкий предел сопротивления IR.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в МОм).</param>
    public static async Task SetLowResistanceLimitAsync(Model model, double value)
    {
      string query = string.Empty;
      if (value == 1000)
      {
        value = 999;
      }
      query = $"{GetCommandSyntax(ManualCommand.MANU_IR_RLOSET)} {value:F0}M";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает время теста IR.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в секундах).</param>
    public static async Task SetTestTimeAsync(Model model, double value)
    {
      var query = $"{GetCommandSyntax(ManualCommand.MANU_IR_TTIME)} {value}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает смещение IR.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в ГОм).</param>
    public static async Task SetOffsetAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем смещение IR: {value} M");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_IR_REF)} {value}M";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Считывает текущую конфигурацию IR.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <returns>Объект с текущими настройками IR.</returns>
    public static async Task<IrConfiguration> ReadConfigurationAsync(Model model)
    {
      try
      {
        LogInformation("Чтение конфигурации режима IR");

        // Чтение напряжения
        var voltageQuery = $"{GetCommandSyntax(ManualCommand.MANU_IR_VOLTAGE)} ?";
        await model.WriteLineAsync(voltageQuery);
        await Task.Delay(10);
        var voltageResponse = await model.ReadLineAsync();
        double voltage = ParseVoltage(voltageResponse);

        // Чтение высокого предела сопротивления
        var rhiQuery = $"{GetCommandSyntax(ManualCommand.MANU_IR_RHISET)} ?";
        await model.WriteLineAsync(rhiQuery);
        var rhiResponse = await model.ReadLineAsync();
        double rhi = ParseResistanceG(rhiResponse);

        // Чтение низкого предела сопротивления
        var rloQuery = $"{GetCommandSyntax(ManualCommand.MANU_IR_RLOSET)} ?";
        await model.WriteLineAsync(rloQuery);
        var rloResponse = await model.ReadLineAsync();
        double rlo = ParseResistanceM(rloResponse);

        // Чтение времени теста
        var timeQuery = $"{GetCommandSyntax(ManualCommand.MANU_IR_TTIME)} ?";
        await model.WriteLineAsync(timeQuery);
        var timeResponse = await model.ReadLineAsync();
        double time = ParseTime(timeResponse);

        // Чтение смещения
        var refQuery = $"{GetCommandSyntax(ManualCommand.MANU_IR_REF)} ?";
        await model.WriteLineAsync(refQuery);
        var refResponse = await model.ReadLineAsync();
        double reference = ParseResistanceG(refResponse);

        // Возвращаем объект конфигурации
        return new IrConfiguration
        {
          Voltage = voltage,
          HighResistanceLimit = rhi,
          LowResistanceLimit = rlo,
          TestTime = time,
          Offset = reference
        };
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при чтении конфигурации IR: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Парсит значение напряжения из строки ответа устройства.
    /// </summary>
    private static double ParseVoltage(string response)
    {
      string numericPart = response.Replace("kV", "").Trim().Replace(".", ",");
      if (double.TryParse(numericPart, out double voltage))
      {
        return voltage * 1000; // Преобразование кВ в В
      }
      throw new FormatException("Некорректный формат напряжения.");
    }

    /// <summary>
    /// Парсит значение сопротивления из строки ответа устройства.
    /// </summary>
    private static double ParseResistanceG(string response)
    {
      string numericPart = response.Replace("G", "").Trim().Replace(".", ",");
      if (double.TryParse(numericPart, out double resistance))
      {
        return resistance; // Значение уже в ГОм
      }
      return 0.0;
    }

    /// <summary>
    /// Парсит значение сопротивления из строки ответа устройства.
    /// </summary>
    private static double ParseResistanceM(string response)
    {
      string numericPart = response.Replace("M", "").Trim().Replace(".", ",");
      if (double.TryParse(numericPart, out double resistance))
      {
        return resistance; // Значение уже в ГОм
      }
      return 0.0;
    }

    /// <summary>
    /// Парсит значение времени из строки ответа устройства.
    /// </summary>
    private static double ParseTime(string response)
    {
      string numericPart = response.Replace("S", "").Trim().Replace(".", ",");
      if (double.TryParse(numericPart, out double time))
      {
        return time; // Значение уже в секундах
      }
      return 0.0;
    }
  }
}
