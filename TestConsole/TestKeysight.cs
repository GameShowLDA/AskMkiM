using Ask.Core.Shared.DTO.Devices.Measurements;
using System.Globalization;
using TestConsole.Keysight;

namespace TestConsole
{
  internal static class TestKeysight
  {
    public static async Task RunAsync()
    {
      var controller = new KeysightMultimeterController(log: message => Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}"));

      while (true)
      {
        PrintState(controller);
        Console.WriteLine("=== Keysight 34465A ===");
        Console.WriteLine("1. Подключиться");
        Console.WriteLine("2. Инициализировать");
        Console.WriteLine("3. *IDN?");
        Console.WriteLine("4. RESET");
        Console.WriteLine("5. CLEAR STATUS");

        Console.WriteLine("\r\n6. Установить режим сопротивления");
        Console.WriteLine("7. Измерить сопротивление");

        Console.WriteLine("\r\n8. Установить режим постоянного напряжения");
        Console.WriteLine("9. Измерить постоянное напряжение");

        Console.WriteLine("\r\n10. Установить режим переменного напряжения");
        Console.WriteLine("11. Измерить переменное напряжение");

        Console.WriteLine("\r\n12. Установить режим прозвонки");
        Console.WriteLine("13. Проверить прозвонку (true/false)");
        Console.WriteLine("14. Измерить сопротивление прозвонки");

        Console.WriteLine("\r\n15. Установить режим ёмкости");
        Console.WriteLine("16. Измерить ёмкость");

        Console.WriteLine("\r\n17. Установить режим диода");
        Console.WriteLine("18. Измерить диод");

        Console.WriteLine("\r\n19. Пользовательская команда");
        Console.WriteLine("20. Установить IP");
        Console.WriteLine("21. Отключиться");
        Console.WriteLine("22. Set DC voltage range");
        Console.WriteLine("23. Set AC voltage range");
        Console.WriteLine("24. Set resistance range");
        Console.WriteLine("25. Set capacitance range");
        Console.WriteLine("0. Назад");
        Console.Write("Выберите действие: ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 25)
        {
          Console.WriteLine("Неверный выбор.");
          continue;
        }

        switch (choice)
        {
          case 1:
            PrintResult(await controller.ConnectAsync());
            break;
          case 2:
            PrintResult(await controller.InitializeAsync());
            break;
          case 3:
            PrintResult(await controller.IdentifyAsync());
            break;
          case 4:
            PrintResult(await controller.ResetAsync());
            break;
          case 5:
            PrintResult(await controller.ClearStatusAsync());
            break;
          case 6:
            PrintResult(await controller.SetResistanceModeAsync());
            break;
          case 7:
            await PrintMeasurementAsync(() => controller.MeasureResistanceAsync(new MeasurementRange(0, 0, 0)), "Resistance");
            break;
          case 8:
            PrintResult(await controller.SetDcVoltageModeAsync());
            break;
          case 9:
            var dcParameters = ReadVoltageParameters();
            await PrintVoltageMeasurementAsync(
              () => controller.MeasureDcVoltageAsync(new MeasurementRange(dcParameters.Param, dcParameters.RangeFrom, dcParameters.RangeTo)),
              "DC voltage");
            break;
          case 10:
            PrintResult(await controller.SetAcVoltageModeAsync());
            break;
          case 11:
            var acParameters = ReadVoltageParameters();
            await PrintVoltageMeasurementAsync(
              () => controller.MeasureAcVoltageAsync(new MeasurementRange(acParameters.Param, acParameters.RangeFrom, acParameters.RangeTo)),
              "AC voltage");
            break;
          case 12:
            PrintResult(await controller.SetContinuityModeAsync());
            break;
          case 13:
            bool expectedContinuity = ReadBoolean("Ожидаемая прозвонка");
            PrintResult(await controller.CheckContinuityAsync(expectedContinuity));
            break;
          case 14:
            await PrintMeasurementAsync(
              () => controller.MeasureContinuityResistanceAsync(),
              "Continuity resistance");
            break;
          case 15:
            PrintResult(await controller.SetCapacitanceModeAsync());
            break;
          case 16:
            var capacitanceParameters = ReadCapacitanceParameters();
            await PrintMeasurementAsync(
              () => controller.MeasureCapacitanceAsync(new MeasurementRange(capacitanceParameters.Param, capacitanceParameters.RangeFrom, capacitanceParameters.RangeTo)),
              "Capacitance, nF");
            break;
          case 17:
            PrintResult(await controller.SetDiodeModeAsync());
            break;
          case 18:
            await PrintMeasurementAsync(
              () => controller.CheckDiodeAsync(),
              "Diode");
            break;
          case 19:
            await RunCustomCommandAsync(controller);
            break;
          case 20:
            SetConnectionDetails(controller);
            break;
          case 21:
            await controller.DisconnectAsync();
            break;
          case 22:
            PrintResult(await controller.SetDcVoltageRangeAsync(ReadVoltageRange()));
            break;
          case 23:
            PrintResult(await controller.SetAcVoltageRangeAsync(ReadVoltageRange()));
            break;
          case 24:
            PrintResult(await controller.SetResistanceRangeAsync(ReadResistanceRange()));
            break;
          case 25:
            PrintResult(await controller.SetCapacitanceRangeAsync(ReadCapacitanceRange()));
            break;
          case 0:
            return;
        }
      }
    }

    private static void PrintState(KeysightMultimeterController controller)
    {
      Console.WriteLine();
      Console.WriteLine($"Device: {controller.Name}");
      Console.WriteLine($"IP: {controller.ConnectionDetails}");
      Console.WriteLine($"Status: {controller.ConnectionStatus}");
      Console.WriteLine($"Connected: {controller.ConnectionInfo.IsConnected}");
      Console.WriteLine();
    }

    private static async Task RunCustomCommandAsync(KeysightMultimeterController controller)
    {
      Console.Write("Command: ");
      string? command = Console.ReadLine();
      if (string.IsNullOrWhiteSpace(command))
      {
        Console.WriteLine("Command is empty.");
        return;
      }

      int timeoutMs = ReadInt("Timeout ms", 2000);
      int responseDelayMs = ReadInt("Response delay ms", 0);
      int delayBeforeCallMs = ReadInt("Delay before call ms", 0);

      var result = await controller.QueryAsync(
        command,
        responseDelayMs: responseDelayMs,
        timeoutMs: timeoutMs,
        delayBeforeCallMs: delayBeforeCallMs);
      PrintResult(result);
    }

    private static void SetConnectionDetails(KeysightMultimeterController controller)
    {
      Console.Write($"IP address [{controller.ConnectionDetails}]: ");
      string? value = Console.ReadLine();
      if (!string.IsNullOrWhiteSpace(value))
      {
        controller.ConnectionDetails = value.Trim();
      }
    }

    private static bool ReadBoolean(string title)
    {
      Console.Write($"{title} [y/n]: ");
      string? value = Console.ReadLine();
      if (string.IsNullOrWhiteSpace(value))
      {
        return true;
      }

      value = value.Trim();
      return value.Equals("y", StringComparison.OrdinalIgnoreCase)
        || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
        || value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value == "1";
    }

    private static (double Param, double RangeFrom, double RangeTo) ReadVoltageParameters()
    {
      Console.WriteLine("Оставьте значения expected/range без изменений для AUTO range.");
      double param = ReadDouble("Ожидаемое значение", 0);
      double rangeFrom = ReadDouble("Range from", -1);
      double rangeTo = ReadDouble("Range to", -1);
      return (param, rangeFrom, rangeTo);
    }

    private static double ReadVoltageRange()
    {
      return ReadDouble("Voltage range in V (<= 0 for AUTO)", 0);
    }

    private static double ReadResistanceRange()
    {
      return ReadDouble("Resistance range in Ohm (<= 0 for AUTO)", 0);
    }

    private static double ReadCapacitanceRange()
    {
      return ReadDouble("Capacitance range in nF (<= 0 for AUTO)", 0);
    }

    private static (double Param, double RangeFrom, double RangeTo) ReadCapacitanceParameters()
    {
      Console.WriteLine("Значения вводятся в nF. Оставьте expected/range без изменений для AUTO range.");
      double param = ReadDouble("Ожидаемое значение", 0);
      double rangeFrom = ReadDouble("Range from", -1);
      double rangeTo = ReadDouble("Range to", -1);
      return (param, rangeFrom, rangeTo);
    }

    private static async Task PrintMeasurementAsync(Func<Task<double>> measureAsync, string title)
    {
      try
      {
        double value = await measureAsync();
        Console.WriteLine($"{title}: {value.ToString("G17", CultureInfo.InvariantCulture)}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error: {ex.Message}");
      }
    }

    private static async Task PrintVoltageMeasurementAsync(Func<Task<double>> measureAsync, string title)
    {
      try
      {
        double valueInVolts = await measureAsync();
        Console.WriteLine($"{title}: {FormatVolts(valueInVolts)} V");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error: {ex.Message}");
      }
    }

    private static string FormatVolts(double value)
    {
      if (double.IsNaN(value) || double.IsInfinity(value))
      {
        return value.ToString(CultureInfo.InvariantCulture);
      }

      return value.ToString("0.#################", CultureInfo.InvariantCulture);
    }

    private static void PrintResult(KeysightCommandResult result)
    {
      Console.WriteLine($"Success: {result.Success}");
      Console.WriteLine($"TimedOut: {result.TimedOut}");
      Console.WriteLine($"Elapsed: {result.Elapsed.TotalMilliseconds:F0} ms");
      Console.WriteLine($"Response: {result.Response}");
      if (!result.Success)
      {
        Console.WriteLine($"Error: {result.ErrorMessage}");
      }
    }

    private static int ReadInt(string title, int defaultValue)
    {
      Console.Write($"{title} [{defaultValue}]: ");
      string? value = Console.ReadLine();
      return int.TryParse(value, out int result) ? result : defaultValue;
    }

    private static double ReadDouble(string title, double defaultValue)
    {
      Console.Write($"{title} [{defaultValue.ToString("G", CultureInfo.InvariantCulture)}]: ");
      string? value = Console.ReadLine();
      if (string.IsNullOrWhiteSpace(value))
      {
        return defaultValue;
      }

      value = value.Replace(',', '.');
      return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
        ? result
        : defaultValue;
    }
  }
}
