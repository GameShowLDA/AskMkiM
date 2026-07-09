using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace TestConsole.DeviceBusCommutationConnectorTests
{
  internal static class DeviceBusCommutationConnectorTest
  {
    public static async Task RunAsync()
    {
      var controller = new DeviceBusCommutationConnectorController(log: message => Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}"));

      while (true)
      {
        PrintState(controller);
        Console.WriteLine("=== DeviceBusCommutation ConnectorManager ===");
        Console.WriteLine("1. Connect");
        Console.WriteLine("2. Initialize");
        Console.WriteLine("3. Reset");
        Console.WriteLine("4. Disconnect");
        Console.WriteLine("5. Set IP");
        Console.WriteLine("6. Set chassis/device number");

        Console.WriteLine("\r\nMultimeter:");
        Console.WriteLine("7. Connect multimeter");
        Console.WriteLine("8. Disconnect multimeter");

        Console.WriteLine("\r\nADC:");
        Console.WriteLine("9. Connect ADC");
        Console.WriteLine("10. Disconnect ADC");
        Console.WriteLine("11. Connect ADC reversed");
        Console.WriteLine("12. Disconnect ADC reversed");

        Console.WriteLine("\r\nPINT:");
        Console.WriteLine("13. Connect PINT");
        Console.WriteLine("14. Disconnect PINT");

        Console.WriteLine("\r\nBreakdown tester:");
        Console.WriteLine("15. Connect breakdown tester");
        Console.WriteLine("16. Disconnect breakdown tester");
        Console.WriteLine("17. Connect breakdown tester and multimeter");
        Console.WriteLine("18. Disconnect breakdown tester and multimeter");

        Console.WriteLine("\r\nBuses/divider:");
        Console.WriteLine("19. Connect all buses");
        Console.WriteLine("20. Disconnect all buses");
        Console.WriteLine("21. Enable divider");
        Console.WriteLine("22. Disable divider");
        Console.WriteLine("23. Show connected devices");
        Console.WriteLine("0. Back");
        Console.Write("Select action: ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 23)
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
            PrintResult(await controller.ResetAsync());
            break;
          case 4:
            PrintResult(await controller.DisconnectAsync());
            break;
          case 5:
            SetConnectionDetails(controller);
            break;
          case 6:
            SetDeviceNumbers(controller);
            break;
          case 7:
            PrintResult(await controller.ConnectMultimeterAsync(ReadSwitchingBusNew()));
            break;
          case 8:
            PrintResult(await controller.DisconnectMultimeterAsync(ReadSwitchingBusNew()));
            break;
          case 9:
            PrintResult(await controller.ConnectAdcAsync(ReadSwitchingBusNew(), reversePolarity: false));
            break;
          case 10:
            PrintResult(await controller.DisconnectAdcAsync(ReadSwitchingBusNew(), reversePolarity: false));
            break;
          case 11:
            PrintResult(await controller.ConnectAdcAsync(ReadSwitchingBusNew(), reversePolarity: true));
            break;
          case 12:
            PrintResult(await controller.DisconnectAdcAsync(ReadSwitchingBusNew(), reversePolarity: true));
            break;
          case 13:
            PrintResult(await controller.ConnectPintAsync(ReadSwitchingBusNew()));
            break;
          case 14:
            PrintResult(await controller.DisconnectPintAsync(ReadSwitchingBusNew()));
            break;
          case 15:
            PrintResult(await controller.ConnectBreakdownTesterAsync());
            break;
          case 16:
            PrintResult(await controller.DisconnectBreakdownTesterAsync());
            break;
          case 17:
            PrintResult(await controller.ConnectBreakdownTesterAndMultimeterAsync());
            break;
          case 18:
            PrintResult(await controller.DisconnectBreakdownTesterAndMultimeterAsync());
            break;
          case 19:
            PrintResult(await controller.ConnectAllBusesAsync());
            break;
          case 20:
            PrintResult(await controller.DisconnectAllBusesAsync());
            break;
          case 21:
            PrintResult(await controller.EnableDividerAsync());
            break;
          case 22:
            PrintResult(await controller.DisableDividerAsync());
            break;
          case 23:
            PrintConnectedDevices(controller);
            break;
          case 0:
            return;
        }
      }
    }

    private static void PrintState(DeviceBusCommutationConnectorController controller)
    {
      Console.WriteLine();
      Console.WriteLine($"Device: {controller.Name}");
      Console.WriteLine($"IP: {controller.ConnectionDetails}");
      Console.WriteLine($"NumberChassis: {controller.NumberChassis}");
      Console.WriteLine($"Number: {controller.Number}");
      Console.WriteLine($"Connected: {controller.IsConnected}");
      Console.WriteLine();
    }

    private static void SetConnectionDetails(DeviceBusCommutationConnectorController controller)
    {
      Console.Write($"IP address [{controller.ConnectionDetails}]: ");
      string? value = Console.ReadLine();
      if (!string.IsNullOrWhiteSpace(value))
      {
        controller.ConnectionDetails = value.Trim();
      }
    }

    private static void SetDeviceNumbers(DeviceBusCommutationConnectorController controller)
    {
      controller.NumberChassis = ReadInt("NumberChassis", controller.NumberChassis);
      controller.Number = ReadInt("Number", controller.Number);
    }

    private static SwitchingBusNew ReadSwitchingBusNew()
    {
      return ReadEnum("Switching bus", SwitchingBusNew.AB1);
    }

    private static TEnum ReadEnum<TEnum>(string title, TEnum defaultValue) where TEnum : struct, Enum
    {
      Console.WriteLine($"{title} values: {string.Join(", ", Enum.GetNames<TEnum>())}");
      Console.Write($"{title} [{defaultValue}]: ");
      string? value = Console.ReadLine();

      if (string.IsNullOrWhiteSpace(value))
      {
        return defaultValue;
      }

      if (Enum.TryParse(value.Trim(), ignoreCase: true, out TEnum result) && Enum.IsDefined(result))
      {
        return result;
      }

      Console.WriteLine($"Invalid {title}. Using {defaultValue}.");
      return defaultValue;
    }

    private static int ReadInt(string title, int defaultValue)
    {
      Console.Write($"{title} [{defaultValue}]: ");
      string? value = Console.ReadLine();
      return int.TryParse(value, out int result) ? result : defaultValue;
    }

    private static void PrintConnectedDevices(DeviceBusCommutationConnectorController controller)
    {
      var connectedDevices = controller.GetConnectedDevices();
      if (connectedDevices.Count == 0)
      {
        Console.WriteLine("Connected devices: empty");
        return;
      }

      Console.WriteLine("Connected devices:");
      foreach (var device in connectedDevices)
      {
        Console.WriteLine($"- {device.device}: {device.bus}");
      }
    }

    private static void PrintResult(DeviceBusCommutationCommandResult result)
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
  }
}
