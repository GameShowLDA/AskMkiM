using System.Globalization;

namespace TestConsole.B7783
{
  internal static class B7783MultimeterTest
  {
    public static async Task RunAsync()
    {
      var controller = new B7783MultimeterController(log: message => Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}"));

      while (true)
      {
        PrintState(controller);
        Console.WriteLine("=== B7-78/3 multimeter ===");
        Console.WriteLine("1. Connect");
        Console.WriteLine("2. Initialize (*IDN?)");
        Console.WriteLine("3. *IDN?");
        Console.WriteLine("4. READ?");

        Console.WriteLine("\r\n5. Set resistance mode");
        Console.WriteLine("6. Set resistance mode + READ?");

        Console.WriteLine("\r\n7. Set DC voltage mode");
        Console.WriteLine("8. Measure DC voltage");

        Console.WriteLine("\r\n9. Set AC voltage mode");
        Console.WriteLine("10. Measure AC voltage");

        Console.WriteLine("\r\n11. Set continuity mode");
        Console.WriteLine("12. Check continuity (true/false)");
        Console.WriteLine("13. Measure continuity resistance");

        Console.WriteLine("\r\n14. Set capacitance mode");
        Console.WriteLine("15. Measure capacitance");

        Console.WriteLine("\r\n16. Set diode mode");
        Console.WriteLine("17. Measure diode");

        Console.WriteLine("\r\n18. Custom command");
        Console.WriteLine("19. Set USB search pattern");
        Console.WriteLine("20. Disconnect");
        Console.WriteLine("21. Set DC voltage range");
        Console.WriteLine("22. Set AC voltage range");
        Console.WriteLine("0. Back");
        Console.Write("Select action: ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 22)
        {
          Console.WriteLine("Invalid selection.");
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
            PrintResult(await controller.ReadAsync());
            break;
          case 5:
            PrintResult(await controller.SetResistanceModeAsync());
            break;
          case 6:
            await PrintMeasurementAsync(() => controller.MeasureResistanceAsync(), "Resistance");
            break;
          case 7:
            PrintResult(await controller.SetDcVoltageModeAsync());
            break;
          case 8:
            var dcParameters = ReadVoltageParameters();
            await PrintVoltageMeasurementAsync(
              () => controller.MeasureDcVoltageAsync(dcParameters.Param, dcParameters.RangeFrom, dcParameters.RangeTo),
              "DC voltage");
            break;
          case 9:
            PrintResult(await controller.SetAcVoltageModeAsync());
            break;
          case 10:
            var acParameters = ReadVoltageParameters();
            await PrintVoltageMeasurementAsync(
              () => controller.MeasureAcVoltageAsync(acParameters.Param, acParameters.RangeFrom, acParameters.RangeTo),
              "AC voltage");
            break;
          case 11:
            PrintResult(await controller.SetContinuityModeAsync());
            break;
          case 12:
            bool expectedContinuity = ReadBoolean("Expected continuity");
            PrintResult(await controller.CheckContinuityAsync(expectedContinuity));
            break;
          case 13:
            PrintResult(await controller.MeasureContinuityResistanceAsync());
            break;
          case 14:
            PrintResult(await controller.SetCapacitanceModeAsync());
            break;
          case 15:
            var capacitanceParameters = ReadCapacitanceParameters();
            await PrintMeasurementAsync(
              () => controller.MeasureCapacitanceAsync(capacitanceParameters.Param, capacitanceParameters.RangeFrom, capacitanceParameters.RangeTo),
              "Capacitance, nF");
            break;

          case 16:
            PrintResult(await controller.SetDiodeModeAsync());
            break;
          case 17:
            PrintResult(await controller.MeasureDiodeAsync());
            break;

          case 18:
            await RunCustomCommandAsync(controller);
            break;
          case 19:
            SetConnectionDetails(controller);
            break;
          case 20:
            await controller.DisconnectAsync();
            break;
          case 21:
            PrintResult(await controller.SetDcVoltageRangeAsync(ReadVoltageRange()));
            break;
          case 22:
            PrintResult(await controller.SetAcVoltageRangeAsync(ReadVoltageRange()));
            break;
          case 0:
            return;
        }
      }
    }

    private static (double Param, double RangeFrom, double RangeTo) ReadVoltageParameters()
    {
      Console.WriteLine("Leave expected/range values unchanged for AUTO range.");
      double param = ReadDouble("Expected value", 0);
      double rangeFrom = ReadDouble("Range from", -1);
      double rangeTo = ReadDouble("Range to", -1);
      return (param, rangeFrom, rangeTo);
    }

    private static double ReadVoltageRange()
    {
      return ReadDouble("Voltage range in V (<= 0 for AUTO)", 0);
    }

    private static (double Param, double RangeFrom, double RangeTo) ReadCapacitanceParameters()
    {
      Console.WriteLine("Values are in nF. Leave expected/range values unchanged for AUTO range.");
      double param = ReadDouble("Expected value", 0);
      double rangeFrom = ReadDouble("Range from", -1);
      double rangeTo = ReadDouble("Range to", -1);
      return (param, rangeFrom, rangeTo);
    }

    private static void PrintState(B7783MultimeterController controller)
    {
      Console.WriteLine();
      Console.WriteLine($"Device: {controller.Name}");
      Console.WriteLine($"ConnectionDetails: {controller.ConnectionDetails}");
      Console.WriteLine($"LastResolvedDevicePath: {controller.LastResolvedDevicePath}");
      Console.WriteLine($"Status: {controller.ConnectionStatus}");
      Console.WriteLine();
    }

    private static async Task RunCustomCommandAsync(B7783MultimeterController controller)
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

    private static void SetConnectionDetails(B7783MultimeterController controller)
    {
      Console.Write($"USB search pattern [{controller.ConnectionDetails}]: ");
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

    private static void PrintResult(B7783CommandResult result)
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
