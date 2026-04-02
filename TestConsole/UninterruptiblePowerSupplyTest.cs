using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Device.Runtime.Device;
using DataBaseConfiguration.Services.Device;

namespace TestConsole
{
  internal static class UninterruptiblePowerSupplyTest
  {
    public static async Task RunAsync()
    {
      Console.WriteLine("=== Отладка UPS ===");

      IUninterruptiblePowerSupply? device = SelectDevice();
      if (device == null)
      {
        return;
      }

      while (true)
      {
        PrintDeviceState(device);
        Console.WriteLine("1. Инициализация");
        Console.WriteLine("2. Подключение");
        Console.WriteLine("3. Проверка питания");
        Console.WriteLine("4. Включение питания");
        Console.WriteLine("5. Отключение питания");
        Console.WriteLine("6. Смена питания");
        Console.WriteLine("7. Сброс");
        Console.WriteLine("8. Отключение устройства");
        Console.WriteLine("9. Сменить устройство");
        Console.WriteLine("0. Выход");
        Console.Write("Введите номер действия: ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 9)
        {
          Console.WriteLine("Неверный выбор. Попробуйте снова.");
          continue;
        }

        switch (choice)
        {
          case 1:
            await RunConnectActionAsync("Инициализация", () => device.ConnectableManager.InitializeAsync(), device);
            break;

          case 2:
            await RunConnectActionAsync("Подключение", () => device.ConnectableManager.ConnectAsync(), device);
            break;

          case 3:
            await RunBooleanActionAsync("Проверка питания", () => device.VerifyPowerAsync(), device);
            break;

          case 4:
            await RunActionAsync("Включение питания", () => device.StartPowerAsync(), device);
            break;

          case 5:
            await RunActionAsync("Отключение питания", () => device.StopPowerAsync(), device);
            break;

          case 6:
            await TogglePowerAsync(device);
            break;

          case 7:
            await RunBooleanActionAsync("Сброс", () => device.ConnectableManager.ResetAsync(), device);
            break;

          case 8:
            await RunBooleanActionAsync("Отключение устройства", () => device.ConnectableManager.DisconnectAsync(), device);
            break;

          case 9:
            device = SelectDevice();
            if (device == null)
            {
              return;
            }

            break;

          case 0:
            return;
        }
      }
    }

    private static IUninterruptiblePowerSupply? SelectDevice()
    {
      while (true)
      {
        Console.WriteLine();
        Console.WriteLine("Выберите источник устройства:");
        Console.WriteLine("1. Выбрать UPS из БД");
        Console.WriteLine("2. Использовать встроенный MIK-UPS-1101R-RM");
        Console.WriteLine("0. Назад");
        Console.Write("Введите номер действия: ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 2)
        {
          Console.WriteLine("Неверный выбор. Попробуйте снова.");
          continue;
        }

        switch (choice)
        {
          case 1:
            return SelectDeviceFromDatabase();

          case 2:
            return new MikUps1101rRmDevice
            {
              NumberChassis = 1,
              Number = 1,
            };

          case 0:
            return null;
        }
      }
    }

    private static IUninterruptiblePowerSupply? SelectDeviceFromDatabase()
    {
      var devices = new UninterruptiblePowerSupplyServices().GetAll();

      if (devices == null || devices.Count == 0)
      {
        Console.WriteLine("В БД нет настроенных UPS.");
        return null;
      }

      Console.WriteLine("Доступные UPS:");
      for (int i = 0; i < devices.Count; i++)
      {
        var device = devices[i];
        Console.WriteLine($"{i + 1}. {device.Name} (Шасси: {device.NumberChassis}, Устройство: {device.Number}, Connection: {device.ConnectionDetails})");
      }

      Console.Write("Введите номер устройства: ");
      if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > devices.Count)
      {
        Console.WriteLine("Некорректный выбор.");
        return null;
      }

      return devices[choice - 1];
    }

    private static async Task RunActionAsync(string title, Func<Task> action, IUninterruptiblePowerSupply device)
    {
      try
      {
        Console.WriteLine($"--- {title} ---");
        await action();
        Console.WriteLine("Операция выполнена.");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка: {ex.Message}");
      }

      PrintDeviceState(device);
    }

    private static async Task RunBooleanActionAsync(string title, Func<Task<bool>> action, IUninterruptiblePowerSupply device)
    {
      try
      {
        Console.WriteLine($"--- {title} ---");
        bool result = await action();
        Console.WriteLine($"Результат: {result}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка: {ex.Message}");
      }

      PrintDeviceState(device);
    }

    private static async Task RunConnectActionAsync(string title, Func<Task<(bool Connect, string Answer)>> action, IUninterruptiblePowerSupply device)
    {
      try
      {
        Console.WriteLine($"--- {title} ---");
        var result = await action();
        Console.WriteLine($"Connect: {result.Connect}");
        Console.WriteLine($"Answer: {result.Answer}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка: {ex.Message}");
      }

      PrintDeviceState(device);
    }

    private static async Task TogglePowerAsync(IUninterruptiblePowerSupply device)
    {
      try
      {
        Console.WriteLine("--- Смена питания ---");
        bool hasPower = await device.VerifyPowerAsync();
        Console.WriteLine($"Текущее состояние: {(hasPower ? "Включено" : "Выключено")}");

        if (hasPower)
        {
          await device.StopPowerAsync();
          Console.WriteLine("Выполнено отключение питания.");
        }
        else
        {
          await device.StartPowerAsync();
          Console.WriteLine("Выполнено включение питания.");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка: {ex.Message}");
      }

      PrintDeviceState(device);
    }

    private static void PrintDeviceState(IUninterruptiblePowerSupply device)
    {
      Console.WriteLine();
      Console.WriteLine($"Устройство: {device.Name}");
      Console.WriteLine($"Шасси: {device.NumberChassis}");
      Console.WriteLine($"Номер: {device.Number}");
      Console.WriteLine($"ConnectionDetails: {device.ConnectionDetails}");
      Console.WriteLine($"LastResolvedDevicePath: {device.LastResolvedDevicePath}");
      Console.WriteLine($"ConnectionStatus: {device.ConnectableManager.GetConnectionStatus()}");
      Console.WriteLine();
    }
  }
}
