using System.Net.Sockets;
using System.Text;

namespace TestConsole.B5108;

internal static partial class B5108Probe
{
  public static async Task RunMenuAsync()
  {
    while (true)
    {
      Console.WriteLine("\nБ5-108 — 192.168.1.17:80");
      Console.WriteLine("1. Проверка команд чтения (SCPI-запросы и ошибки)");
      Console.WriteLine("2. Проверка параметров (шаг, минимум, максимум; выход выключен)");
      Console.WriteLine("3. Проверка параметров и включения на минимальных уставках");
      Console.WriteLine("4. Ручной ввод SCPI-команд");
      Console.WriteLine("5. Установка U/I: шаг 0,1 В / 1 мА (выход выключен)");
      Console.WriteLine("0. Назад");
      Console.Write("Выберите действие: ");
      string? choice = Console.ReadLine()?.Trim();
      if (choice is null or "0") return;
      string[] options;
      switch (choice)
      {
        case "1": options = []; break;
        case "2": options = ["--characterize", "--display"]; break;
        case "3": options = ["--characterize", "--output-test", "--display"]; break;
        case "4": options = ["--interactive"]; break;
        case "5": options = ["--settings"]; break;
        default:
          Console.WriteLine("Выберите пункт 0–5.");
          continue;
      }
      await RunAsync(["192.168.1.17", "80", .. options]);
      Console.WriteLine("\nПроверка завершена. Enter — вернуться в меню Б5-108.");
      if (Console.ReadLine() is null) return;
    }
  }

  private static readonly string[] Queries =
  [
    "*IDN?", "SYST:VERS?", "VOLT?", "VOLT:LIM?", "CURR?", "CURR:LIM?",
    "MEAS:VOLT?", "MEAS:CURR?", "OUTP?", "OUTP:PON?", "STAT:OPER:COND?",
    "CAL:STAT?", "SYST:COMM:GPIB:ADDR?", "SYST:COMM:LAN:IPAD?", "SYST:COMM:LAN:PORT?",
    "SYST:COMM:LAN:SMAS?", "SYST:COMM:LAN:GAT?", "SYST:COMM:LAN:MAC?"
  ];

  public static async Task<int> RunAsync(string[] args)
  {
    string host = args.Length > 0 ? args[0] : "192.168.1.17";
    int port = args.Length > 1 ? int.Parse(args[1]) : 80;
    bool interactive = args.Any(a => a.Equals("--interactive", StringComparison.OrdinalIgnoreCase));
    Console.WriteLine($"Б5-108: TCP {host}:{port}; ASCII, LF; timeout 3 s.");
    try
    {
      using var client = new TcpClient();
      using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
        await client.ConnectAsync(host, port, timeout.Token);
      using var stream = client.GetStream();
      string identity = await ExchangeAsync(stream, "*IDN?");
      if (!identity.Contains("B5-108", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException($"Ожидался B5-108, получен ответ: {identity}");
      Console.WriteLine("Очередь ошибок до проверки (чтение извлекает ошибки из очереди):");
      await DrainErrorsAsync(stream);
      if (args.Contains("--settings"))
      {
        await RunSettingsAsync(stream);
        return 0;
      }
      if (args.Contains("--characterize") || args.Contains("--output-test"))
      {
        await CharacterizeAsync(stream, args.Contains("--output-test"), args.Contains("--display") ? 1500 : 0);
        return 0;
      }
      foreach (string query in Queries.Skip(1))
      {
        await ExchangeAsync(stream, query);
        await DrainErrorsAsync(stream);
      }
      if (interactive)
      {
        Console.WriteLine("Ручной SCPI: команды без ? меняют прибор. Пустая строка — выход.");
        while (true)
        {
          Console.Write("SCPI> ");
          string? command = Console.ReadLine();
          if (string.IsNullOrWhiteSpace(command)) break;
          await ExchangeAsync(stream, command);
          await DrainErrorsAsync(stream);
        }
      }
      return 0;
    }
    catch (Exception error) when (error is SocketException or IOException or OperationCanceledException or InvalidOperationException)
    {
      Console.Error.WriteLine($"Проверка остановлена: {error.Message}");
      return 1;
    }
  }

  private static async Task DrainErrorsAsync(NetworkStream stream)
  {
    for (int i = 0; i < 32; i++)
    {
      string error = await ExchangeAsync(stream, "SYST:ERR?");
      if (error == "0" || error.StartsWith("0,")) return;
    }
    throw new InvalidOperationException("Очередь ошибок не опустела после 32 запросов.");
  }

  private static async Task<string> ExchangeAsync(NetworkStream stream, string command)
  {
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    Console.WriteLine($"> {command}");
    await stream.WriteAsync(Encoding.ASCII.GetBytes(command + "\n"), timeout.Token);
    if (!command.Contains('?')) return string.Empty;
    var answer = new StringBuilder();
    byte[] buffer = new byte[1];
    while (answer.Length < 8192)
    {
      int count = await stream.ReadAsync(buffer, timeout.Token);
      if (count == 0) throw new IOException("Прибор закрыл соединение до терминатора LF.");
      if (buffer[0] == 10)
      {
        string result = answer.ToString().Trim();
        Console.WriteLine($"< {result}");
        return result;
      }
      answer.Append((char)buffer[0]);
    }
    throw new IOException("Ответ превысил 8192 символа без терминатора LF.");
  }
}
