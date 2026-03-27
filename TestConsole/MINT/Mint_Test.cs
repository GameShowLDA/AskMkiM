using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Device.Runtime.Commands;
using DataBaseConfiguration.Services.Device;


namespace TestConsole.MINT
{
  public partial class Mint_Test
  {
    internal static async Task RunAsync()
    {
      Console.WriteLine("=== Самоконтроль МИНТ ===");
      while (true)
      {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("1. Весь самоконтроль");
        Console.WriteLine("0. Выход");

        Console.Write("Введите номер действия: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 8)
        {
          Console.WriteLine("Неверный выбор. Попробуйте снова.");
          continue;
        }

        switch (choice)
        {
          case 1:
            await SelfCheck();
            break;

          case 0:
            return;

          default:
            Console.WriteLine("Неверный выбор. Попробуйте снова.");
            break;
        }
      }
    }

    private static async Task SelfCheck()
    {
      IChassisManager chassisManager = GetDeviceInstance(SelectManagerChassis);
      ISwitchingDevice dbc = GetDeviceInstance(SelectDeviceBusCommutationAsync);
      IFastMeter meter = GetDeviceInstance(SelectMeter);
      IPowerSourceModule powerSource = GetDeviceInstance(SelectPowerSource);

      await chassisManager.PowerManager.StartPowerAsync();
      await Task.Delay(5000);

      if (!await CheckConnectionsAsync(dbc, meter, powerSource))
      {
        return;
      }

      await dbc.ConnectableManager.ResetAsync();
      await powerSource.ConnectableManager.ResetAsync();

      await SettingsMeter(meter);
      await powerSource.BusManager.ConnectBusToPositiveAsync(SwitchingBus.A2);
      await powerSource.BusManager.ConnectBusToNegativeAsync(SwitchingBus.B2);
      await dbc.DeviceProtocol.QueryAsync(new DeviceCommand(5, 2, 2, 1).ToString());
      await GenerateDiscreteVoltageCheck(meter, powerSource);
      await CheckMintSwitching(meter, powerSource, dbc);

      await dbc.ConnectableManager.ResetAsync();
      await powerSource.ConnectableManager.ResetAsync();
    }

    private static ISwitchingDevice SelectDeviceBusCommutationAsync()
    {
      var dbc = SwitchingDevices.GetAllAsync().GetAwaiter().GetResult();

      if (dbc == null || !dbc.Any())
      {
        Console.WriteLine("Нет доступных устройств.");
        return null;
      }

      Console.WriteLine("Выберите устройство для самоконтроля блокировочных реле:");

      int index = 1;
      foreach (var device in dbc)
      {
        Console.WriteLine($"{index}. {device.Name} (Номер шасси: {device.NumberChassis}, Номер устройства: {device.Number})");
        index++;
      }

      Console.Write("Введите номер устройства: ");
      if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= dbc.Count)
      {
        var selectedDevice = dbc.ElementAt(choice - 1);
        Console.WriteLine($"Выбрано устройство: {selectedDevice.Name} (ID: {selectedDevice.Id})");
        return selectedDevice;
      }
      else
      {
        Console.WriteLine("Некорректный выбор.");
      }

      return null;
    }
    private static IChassisManager SelectManagerChassis()
    {
      var dbc = ChassisManagers.GetAllAsync().GetAwaiter().GetResult();

      if (dbc == null || !dbc.Any())
      {
        Console.WriteLine("Нет доступных устройств.");
        return null;
      }

      Console.WriteLine("Выберите устройство для самоконтроля блокировочных реле:");

      int index = 1;
      foreach (var device in dbc)
      {
        Console.WriteLine($"{index}. {device.Name} (Номер устройства: {device.Number})");
        index++;
      }

      Console.Write("Введите номер устройства: ");
      if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= dbc.Count)
      {
        var selectedDevice = dbc.ElementAt(choice - 1);
        Console.WriteLine($"Выбрано устройство: {selectedDevice.Name} (ID: {selectedDevice.Id})");
        return selectedDevice;
      }
      else
      {
        Console.WriteLine("Некорректный выбор.");
      }

      return null;
    }
    private static IPowerSourceModule SelectPowerSource()
    {
      var dbc = new PowerSourceModuleServices().GetAll();

      if (dbc == null || !dbc.Any())
      {
        Console.WriteLine("Нет доступных устройств.");
        return null;
      }

      Console.WriteLine("Выберите МИНТ:");

      int index = 1;
      foreach (var device in dbc)
      {
        Console.WriteLine($"{index}. {device.Name} (Номер шасси: {device.NumberChassis}, Номер устройства: {device.Number})");
        index++;
      }

      Console.Write("Введите номер устройства: ");
      if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= dbc.Count)
      {
        var selectedDevice = dbc.ElementAt(choice - 1);
        Console.WriteLine($"Выбрано устройство: {selectedDevice.Name} (ID: {selectedDevice.Id})");
        return selectedDevice;
      }
      else
      {
        Console.WriteLine("Некорректный выбор.");
      }

      return null;
    }
    private static IFastMeter SelectMeter()
    {
      var dbc = FastMeters.GetAllAsync().GetAwaiter().GetResult();

      if (dbc == null || !dbc.Any())
      {
        Console.WriteLine("Нет доступных устройств.");
        return null;
      }

      Console.WriteLine("Выберите мультиметр:");

      int index = 1;
      foreach (var device in dbc)
      {
        Console.WriteLine($"{index}. {device.Name} (Номер шасси: {device.NumberChassis}, Номер устройства: {device.Number})");
        index++;
      }

      Console.Write("Введите номер устройства: ");
      if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= dbc.Count)
      {
        var selectedDevice = dbc.ElementAt(choice - 1);
        Console.WriteLine($"Выбрано устройство: {selectedDevice.Name} (ID: {selectedDevice.Id})");
        return selectedDevice;
      }
      else
      {
        Console.WriteLine("Некорректный выбор.");
      }

      return null;
    }

  }
}
