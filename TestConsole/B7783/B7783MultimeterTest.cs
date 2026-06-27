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
        Console.WriteLine("5. Set resistance mode");
        Console.WriteLine("6. Set resistance mode + READ?");
        Console.WriteLine("7. Configure DC voltage + READ?");
        Console.WriteLine("8. Configure AC voltage + READ?");
        Console.WriteLine("9. Custom command");
        Console.WriteLine("10. Set USB search pattern");
        Console.WriteLine("11. Disconnect");
        Console.WriteLine("0. Back");
        Console.Write("Select action: ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 11)
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
            await PrintMeasurementAsync(() => controller.MeasureDcVoltageAsync(), "DC voltage");
            break;
          case 8:
            await PrintMeasurementAsync(() => controller.MeasureAcVoltageAsync(), "AC voltage");
            break;
          case 9:
            await RunCustomCommandAsync(controller);
            break;
          case 10:
            SetConnectionDetails(controller);
            break;
          case 11:
            await controller.DisconnectAsync();
            break;
          case 0:
            return;
        }
      }
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

    private static async Task PrintMeasurementAsync(Func<Task<double>> measureAsync, string title)
    {
      try
      {
        double value = await measureAsync();
        Console.WriteLine($"{title}: {value}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error: {ex.Message}");
      }
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
  }
}
