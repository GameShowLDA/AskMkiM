using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TestConsole.Keysight.Stress;

internal static class KeysightStressTest
{
  public static async Task<int> RunAsync(string[] args)
  {
    Console.OutputEncoding = Encoding.UTF8;
    if (args.Contains("--help"))
    {
      Console.WriteLine("TestConsole keysight-stress [IP] [--port 5025] [--timeout 10000] [--cycles 0] [--delay 0] [--log path]");
      Console.WriteLine("cycles=0: непрерывно; delay: пауза между циклами, мс; остановка: Ctrl+C.");
      return 0;
    }

    try
    {
      var options = Options.Parse(args);
      using var cancellation = new CancellationTokenSource();
      ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
      Console.CancelKeyPress += cancel;
      try
      {
        using var journal = new Journal(options.LogPath);
        using var connection = new ScpiConnection(options.Host, options.Port, options.Timeout, journal);
        journal.Write("START", $"{options.Host}:{options.Port}; timeout={options.Timeout}ms; cycles={options.Cycles}; delay={options.Delay}ms");
        Console.WriteLine($"Журнал: {Path.GetFullPath(options.LogPath)}\nОстановка: Ctrl+C");
        int cycles = 0, succeeded = 0, failed = 0;
        var elapsed = Stopwatch.StartNew();
        try
        {
          while (options.Cycles == 0 || cycles < options.Cycles)
          {
            cancellation.Token.ThrowIfCancellationRequested();
            cycles++;
            try
            {
              if (!connection.Ready)
              {
                var identification = await connection.ExchangeAsync("*IDN?", true, cancellation.Token);
                if (!identification.Contains("34465A", StringComparison.OrdinalIgnoreCase))
                  throw new InvalidDataException($"Ожидался Keysight 34465A, получено: {identification}");
                await connection.ExchangeAsync("CONF:CAP", false, cancellation.Token);
                var mode = await connection.ExchangeAsync("FUNC?", true, cancellation.Token);
                if (!mode.Trim('"').Equals("CAP", StringComparison.OrdinalIgnoreCase))
                  throw new InvalidDataException($"Ожидался режим CAP, получено: {mode}");
                connection.Ready = true;
                journal.Write("WAIT", "Инициализация успешна. Измерения начнутся через 5 секунд — подключите конденсатор.");
                await Task.Delay(5000, cancellation.Token);
              }

              string answer = await MeasureAsync(connection, cancellation.Token);
              if (!double.TryParse(answer, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                || !double.IsFinite(value))
                throw new InvalidDataException($"Некорректный результат измерения: {answer}");
              succeeded++;
              journal.Write("MEASUREMENT", $"cycle={cycles}; value={answer} F; overload={Math.Abs(value) >= 9e37}");
            }
            catch (Exception ex) when (ex is IOException or SocketException or TimeoutException or InvalidDataException)
            {
              failed++;
              connection.Close();
              journal.Write("CYCLE_ERROR", $"cycle={cycles}; {ex}");
              // При недоступном приборе ограничиваем частоту новых подключений.
              await Task.Delay(1000, cancellation.Token);
            }

            journal.Write("STATUS", $"cycles={cycles}; ok={succeeded}; failed={failed}; queries={connection.Queries}; transportErrors={connection.Errors}; elapsed={elapsed.Elapsed}");
            if (options.Delay > 0)
              await Task.Delay(options.Delay, cancellation.Token);
          }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
          journal.Write("STOP", "Остановлено оператором.");
        }
        finally
        {
          journal.Write("SUMMARY", $"cycles={cycles}; ok={succeeded}; failed={failed}; queries={connection.Queries}; transportErrors={connection.Errors}; elapsed={elapsed.Elapsed}");
        }
        return failed > 0 || connection.Errors > 0 ? 1 : 0;
      }
      finally
      {
        Console.CancelKeyPress -= cancel;
      }
    }
    catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
    {
      Console.Error.WriteLine(ex.Message);
      return 2;
    }
  }

  private static async Task<string> MeasureAsync(ScpiConnection connection, CancellationToken token)
  {
    for (int attempt = 1; attempt <= 4; attempt++)
    {
      try
      {
        return await connection.ExchangeAsync("MEAS:CAP?", true, token);
      }
      catch (Exception ex) when (attempt < 4 && ex is IOException or SocketException or TimeoutException)
      {
        if (attempt == 2)
        {
          string identification = await connection.ExchangeAsync("*IDN?", true, token);
          if (!identification.Contains("34465A", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Некорректный ответ идентификации: {identification}");
        }
      }
    }
    throw new InvalidOperationException("Исчерпаны попытки измерения.");
  }
}

internal sealed record Options(string Host, int Port, int Timeout, int Cycles, int Delay, string LogPath)
{
  public static Options Parse(string[] args)
  {
    string? host = null;
    int port = 5025, timeout = 10000, cycles = 0, delay = 0;
    string path = Path.Combine(AppContext.BaseDirectory, "logs", $"keysight-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.jsonl");
    for (int i = 0; i < args.Length; i++)
    {
      string key = args[i];
      if (!key.StartsWith("--", StringComparison.Ordinal))
      {
        if (host != null) throw new ArgumentException("Укажите только один IP/хост.");
        host = key;
        continue;
      }
      if (i + 1 >= args.Length) throw new ArgumentException($"Нет значения для {key}.");
      string value = args[++i];
      if (key == "--log") { path = value; continue; }
      if (!int.TryParse(value, out int number)) throw new ArgumentException($"Некорректное число для {key}.");
      switch (key)
      {
        case "--port" when number is > 0 and <= 65535: port = number; break;
        case "--timeout" when number > 0: timeout = number; break;
        case "--cycles" when number >= 0: cycles = number; break;
        case "--delay" when number >= 0: delay = number; break;
        default: throw new ArgumentException($"Неизвестный параметр или недопустимое значение: {key} {value}");
      }
    }
    if (host == null)
    {
      Console.Write("IP-адрес Keysight 34465A: ");
      host = Console.ReadLine();
    }
    if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("IP/хост не задан.");
    return new(host.Trim(), port, timeout, cycles, delay, path);
  }
}

internal sealed class Journal : IDisposable
{
  private readonly StreamWriter writer;

  public Journal(string path)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
  }

  public void Write(string kind, string message)
  {
    var time = DateTimeOffset.Now;
    writer.WriteLine(JsonSerializer.Serialize(new { time, kind, message }));
    Console.WriteLine($"{time:HH:mm:ss.fff} [{kind}] {message}");
  }

  public void Dispose() => writer.Dispose();
}

internal sealed class ScpiConnection(string host, int port, int timeout, Journal journal) : IDisposable
{
  private TcpClient? client;
  private NetworkStream? stream;
  public bool Ready { get; set; }
  public long Queries { get; private set; }
  public long Errors { get; private set; }

  public async Task<string> ExchangeAsync(string command, bool read, CancellationToken token)
  {
    var elapsed = Stopwatch.StartNew();
    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
    deadline.CancelAfter(timeout);
    Queries++;
    try
    {
      if (client == null)
      {
        client = new TcpClient();
        await client.ConnectAsync(host, port, deadline.Token);
        stream = client.GetStream();
        journal.Write("CONNECT", $"{host}:{port}");
      }
      journal.Write("TX", command);
      await stream!.WriteAsync(Encoding.ASCII.GetBytes(command + "\n"), deadline.Token);
      if (!read) return string.Empty;
      // TCP не сохраняет границы сообщений: ждём SCPI-терминатор, а не первый пакет.
      var response = new StringBuilder();
      byte[] buffer = new byte[1];
      while (response.Length < 65536)
      {
        if (await stream.ReadAsync(buffer, deadline.Token) == 0)
          throw new EndOfStreamException($"Соединение закрыто во время {command}; partial={response}");
        if (buffer[0] == '\n')
        {
          string answer = response.ToString().Trim();
          if (answer.Length == 0) throw new InvalidDataException("Пустой ответ SCPI.");
          journal.Write("RX", $"{command}: {answer}; ms={elapsed.Elapsed.TotalMilliseconds:F1}");
          return answer;
        }
        response.Append((char)buffer[0]);
      }
      throw new InvalidDataException("Ответ SCPI превышает 64 КиБ.");
    }
    catch (OperationCanceledException ex) when (!token.IsCancellationRequested)
    {
      Errors++;
      CloseTransport();
      journal.Write("TIMEOUT", $"{command}; ms={elapsed.Elapsed.TotalMilliseconds:F1}; limit={timeout}");
      throw new TimeoutException($"Нет ответа на {command} за {timeout} мс.", ex);
    }
    catch (Exception ex) when (ex is IOException or SocketException or InvalidDataException)
    {
      Errors++;
      CloseTransport();
      journal.Write("TRANSPORT_ERROR", $"{command}; ms={elapsed.Elapsed.TotalMilliseconds:F1}; {ex}");
      throw;
    }
    catch (OperationCanceledException)
    {
      CloseTransport();
      throw;
    }
  }

  private void CloseTransport()
  {
    stream?.Dispose();
    client?.Dispose();
    stream = null;
    client = null;
  }

  public void Close()
  {
    Ready = false;
    CloseTransport();
  }

  public void Dispose() => Close();
}
