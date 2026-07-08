using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace TestConsole.ModuleRelayControlTests
{
  internal static class ModuleRelayControlTest
  {
    public static async Task RunAsync()
    {
      var controller = new ModuleRelayControlController(log: message => Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}"));

      while (true)
      {
        PrintState(controller);
        Console.WriteLine("=== ModuleRelayControl ===");
        Console.WriteLine("1. Connect");
        Console.WriteLine("2. Initialize");
        Console.WriteLine("3. Reset");
        Console.WriteLine("4. Disconnect");
        Console.WriteLine("5. Set IP");
        Console.WriteLine("6. Set chassis/device/point count");

        Console.WriteLine("\r\nBus:");
        Console.WriteLine("7. Connect bus");
        Console.WriteLine("8. Disconnect bus");
        Console.WriteLine("9. Show connected buses");

        Console.WriteLine("\r\nMeter:");
        Console.WriteLine("10. Connect meter");
        Console.WriteLine("11. Disconnect meter");
        Console.WriteLine("12. Get meter response");

        Console.WriteLine("\r\nPoints:");
        Console.WriteLine("13. Connect relay");
        Console.WriteLine("14. Disconnect relay");
        Console.WriteLine("15. Connect relay verified");
        Console.WriteLine("16. Disconnect relay verified");
        Console.WriteLine("17. Connect relay group");
        Console.WriteLine("18. Disconnect relay group");
        Console.WriteLine("19. Check point");
        Console.WriteLine("20. Connect point to new bus");
        Console.WriteLine("21. Disconnect all points");
        Console.WriteLine("22. Disconnect all points from bus A");
        Console.WriteLine("23. Disconnect all points from bus B");
        Console.WriteLine("24. Show connected points");
        Console.WriteLine("0. Back");
        Console.Write("Select action: ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 24)
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
            PrintResult(await controller.ConnectBusAsync(ReadSwitchingBus()));
            break;
          case 8:
            PrintResult(await controller.DisconnectBusAsync(ReadSwitchingBus()));
            break;
          case 9:
            PrintConnectedBuses(controller);
            break;
          case 10:
            PrintResult(await controller.ConnectMeterAsync());
            break;
          case 11:
            PrintResult(await controller.DisconnectMeterAsync());
            break;
          case 12:
            PrintResult(await controller.GetMeterResponseAsync());
            break;
          case 13:
            {
              var point = ReadPointOperation();
              PrintResult(await controller.ConnectRelayAsync(point.Bus, point.Point));
              break;
            }
          case 14:
            {
              var point = ReadPointOperation();
              PrintResult(await controller.DisconnectRelayAsync(point.Bus, point.Point));
              break;
            }
          case 15:
            {
              var point = ReadPointOperation();
              PrintResult(await controller.ConnectRelayVerifiedAsync(point.Bus, point.Point));
              break;
            }
          case 16:
            {
              var point = ReadPointOperation();
              PrintResult(await controller.DisconnectRelayVerifiedAsync(point.Bus, point.Point));
              break;
            }
          case 17:
            {
              var group = ReadPointGroupOperation();
              PrintResult(await controller.ConnectRelayGroupAsync(group.Bus, group.FirstPoint, group.LastPoint));
              break;
            }
          case 18:
            {
              var group = ReadPointGroupOperation();
              PrintResult(await controller.DisconnectRelayGroupAsync(group.Bus, group.FirstPoint, group.LastPoint));
              break;
            }
          case 19:
            PrintResult(await controller.CheckPointAsync(ReadInt("Point", 1)));
            break;
          case 20:
            {
              var point = ReadPointOperation();
              PrintResult(await controller.ConnectPointToNewBusAsync(point.Bus, point.Point));
              break;
            }
          case 21:
            PrintResult(await controller.DisconnectAllPointsAsync());
            break;
          case 22:
            PrintResult(await controller.DisconnectAllPointsFromBusAAsync());
            break;
          case 23:
            PrintResult(await controller.DisconnectAllPointsFromBusBAsync());
            break;
          case 24:
            PrintConnectedPoints(controller);
            break;
          case 0:
            return;
        }
      }
    }

    private static void PrintState(ModuleRelayControlController controller)
    {
      Console.WriteLine();
      Console.WriteLine($"Device: {controller.Name}");
      Console.WriteLine($"IP: {controller.ConnectionDetails}");
      Console.WriteLine($"NumberChassis: {controller.NumberChassis}");
      Console.WriteLine($"Number: {controller.Number}");
      Console.WriteLine($"PointCount: {controller.PointCount}");
      Console.WriteLine($"Connected: {controller.IsConnected}");
      Console.WriteLine();
    }

    private static void SetConnectionDetails(ModuleRelayControlController controller)
    {
      Console.Write($"IP address [{controller.ConnectionDetails}]: ");
      string? value = Console.ReadLine();
      if (!string.IsNullOrWhiteSpace(value))
      {
        controller.ConnectionDetails = value.Trim();
      }
    }

    private static void SetDeviceNumbers(ModuleRelayControlController controller)
    {
      controller.NumberChassis = ReadInt("NumberChassis", controller.NumberChassis);
      controller.Number = ReadInt("Number", controller.Number);
      controller.PointCount = ReadInt("PointCount", controller.PointCount);
    }

    private static SwitchingBus ReadSwitchingBus()
    {
      return ReadEnum("Switching bus", SwitchingBus.AB1);
    }

    private static BusPoint ReadBusPoint()
    {
      return ReadEnum("Point bus", BusPoint.A);
    }

    private static (BusPoint Bus, int Point) ReadPointOperation()
    {
      BusPoint bus = ReadBusPoint();
      int point = ReadInt("Point", 1);
      return (bus, point);
    }

    private static (BusPoint Bus, int FirstPoint, int LastPoint) ReadPointGroupOperation()
    {
      BusPoint bus = ReadBusPoint();
      int firstPoint = ReadInt("First point", 1);
      int lastPoint = ReadInt("Last point", firstPoint);
      return (bus, firstPoint, lastPoint);
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

    private static void PrintConnectedBuses(ModuleRelayControlController controller)
    {
      var buses = controller.GetConnectedBuses();
      if (buses.Count == 0)
      {
        Console.WriteLine("Connected buses: empty");
        return;
      }

      Console.WriteLine("Connected buses:");
      foreach (var bus in buses)
      {
        Console.WriteLine($"- {bus.Bus}: {bus.IsConnected}");
      }
    }

    private static void PrintConnectedPoints(ModuleRelayControlController controller)
    {
      var points = controller.GetConnectedPoints();
      if (points.Count == 0)
      {
        Console.WriteLine("Connected points: empty");
        return;
      }

      Console.WriteLine("Connected points:");
      foreach (var point in points)
      {
        Console.WriteLine($"- Point {point.PointNumber}: {point.Bus}");
      }
    }

    private static void PrintResult(ModuleRelayControlCommandResult result)
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
