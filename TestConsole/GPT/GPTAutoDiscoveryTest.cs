using Ask.Device.Communication.Com.Configuration;
using Ask.Device.Runtime.Device;
using Ask.DataBase.Engine.Static.Devices;
using System.IO.Ports;

namespace TestConsole.GPT
{
  internal static class GPTAutoDiscoveryTest
  {
    private const int ProbeTimeoutMs = 1500;

    public static async Task RunAsync()
    {
      int baudRate = ReadInt("Скорость", 115200);
      string? portName = await FindDeviceAsync(baudRate);
      if (portName == null)
      {
        Console.WriteLine("GPT79904 не найден ни на одном COM-порту.");
        return;
      }

      GPT79904? device = (await BreakdownTesters.GetAllAsync()).OfType<GPT79904>().FirstOrDefault();
      if (device == null)
      {
        Console.WriteLine("GPT79904 не найден в БД. UI также не сможет создать production-экземпляр устройства.");
        return;
      }

      device.ConnectionDetails = CreatePortSettings(portName, baudRate).ToString();
      Console.WriteLine("Для управления загружен тот же экземпляр GPT из BreakdownTesters, который использует UI.");
      await RunControlMenuAsync(device);
    }

    private static async Task<string?> FindDeviceAsync(int baudRate)
    {
      string[] ports = SerialPort.GetPortNames()
        .Where(static name => name.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
        .OrderBy(GetPortNumber)
        .ThenBy(static name => name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

      Console.WriteLine($"Доступные порты: {(ports.Length == 0 ? "нет" : string.Join(", ", ports))}");
      foreach (string portName in ports)
      {
        Console.Write($"Проверка {portName} ({baudRate} 8N1) ... ");
        GPT79904 candidate = CreateDevice(portName, baudRate);

        try
        {
          string response = await candidate.DeviceProtocol.QueryAsync("*IDN?", timeout: ProbeTimeoutMs);
          if (response.Contains("GPT", StringComparison.OrdinalIgnoreCase))
          {
            Console.WriteLine(response);
            Console.WriteLine($"GPT79904 найден на {portName}.");
            if (candidate.COMPort.IsOpen)
            {
              candidate.COMPort.Close();
            }

            return portName;
          }

          Console.WriteLine(string.IsNullOrWhiteSpace(response) ? "нет ответа" : $"другое устройство: {response}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"недоступен: {ex.Message}");
        }
        finally
        {
          if (candidate.COMPort.IsOpen)
          {
            candidate.COMPort.Close();
          }
        }
      }

      return null;
    }

    private static GPT79904 CreateDevice(string portName, int baudRate)
    {
      var device = new GPT79904();
      device.ConnectionDetails = CreatePortSettings(portName, baudRate).ToString();
      return device;
    }

    private static SerialPortCustom CreatePortSettings(string portName, int baudRate)
    {
      return new SerialPortCustom(portName, baudRate, Parity.None, 8, StopBits.One)
      {
        Handshake = Handshake.None,
        Encoding = System.Text.Encoding.ASCII,
        ReadTimeout = 2500,
        WriteTimeout = 2500,
      };
    }

    private static async Task RunControlMenuAsync(GPT79904 device)
    {
      while (true)
      {
        Console.WriteLine();
        Console.WriteLine($"GPT79904: {device.COMPort.PortName}, статус: {device.ConnectionInfo.GetConnectionStatus()}");
        Console.WriteLine("1. Подключить и инициализировать");
        Console.WriteLine("2. *IDN?");
        Console.WriteLine("3. Произвольная SCPI-команда");
        Console.WriteLine("4. Сброс (*RST, *CLS)");
        Console.WriteLine("5. Режим ACW");
        Console.WriteLine("6. Режим DCW");
        Console.WriteLine("7. Режим IR");
        Console.WriteLine("8. Прочитать системную конфигурацию");
        Console.WriteLine("9. Отключить");
        Console.WriteLine("0. Назад");
        Console.Write("Выберите действие: ");

        if (!int.TryParse(Console.ReadLine(), out int choice))
        {
          continue;
        }

        switch (choice)
        {
          case 1:
            var connection = await device.ConnectableManager.ConnectAsync();
            Console.WriteLine(connection.Connect ? "Подключено." : $"Ошибка: {connection.Answer}");
            break;
          case 2:
            await PrintQueryAsync(device, "*IDN?");
            break;
          case 3:
            Console.Write("SCPI: ");
            string? command = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(command))
            {
              await PrintQueryAsync(device, command.Trim());
            }

            break;
          case 4:
            Console.WriteLine(await device.ConnectableManager.ResetAsync() ? "Сброс выполнен." : "Ошибка сброса.");
            break;
          case 5:
            Console.WriteLine((await device.AcwManger.Mode.SetModeAsync()).Message);
            break;
          case 6:
            Console.WriteLine((await device.DcwManger.Mode.SetModeAsync()).Message);
            break;
          case 7:
            Console.WriteLine((await device.IrManger.Mode.SetModeAsync()).Message);
            break;
          case 8:
            await device.SystemManger.ReadConfigurationAsync();
            break;
          case 9:
            Console.WriteLine(await device.ConnectableManager.DisconnectAsync() ? "Отключено." : "Ошибка отключения.");
            break;
          case 0:
            if (device.COMPort.IsOpen)
            {
              await device.ConnectableManager.DisconnectAsync();
            }

            return;
        }
      }
    }

    private static async Task PrintQueryAsync(GPT79904 device, string command)
    {
      string response = await device.DeviceProtocol.QueryAsync(command, timeout: 2500);
      Console.WriteLine(string.IsNullOrWhiteSpace(response) ? "Ответ отсутствует." : $"Ответ: {response}");
    }

    private static int GetPortNumber(string portName)
    {
      return int.TryParse(portName.AsSpan(3), out int number) ? number : int.MaxValue;
    }

    private static int ReadInt(string title, int defaultValue)
    {
      Console.Write($"{title} [{defaultValue}]: ");
      return int.TryParse(Console.ReadLine(), out int value) ? value : defaultValue;
    }
  }
}
