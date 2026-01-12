using System.IO;
using System.IO.Pipes;

namespace MainWindowProgram.Init
{
  /// <summary>
  /// Отвечает за обеспечение единственного экземпляра приложения.
  /// </summary>
  internal static class SingleInstanceManager
  {
    private const string MutexName = "Global\\ASK-MKIM-M-SingleInstance";
    private const string PipeName = "ASK-MKIM-M-Pipe";

    private static Mutex? _mutex;

    public static bool CheckOrSignal()
    {
      bool createdNew;
      _mutex = new Mutex(true, MutexName, out createdNew);

      if (createdNew)
      {
        // Первый экземпляр: запускаем слушатель
        StartPipeServer();
        return true; // продолжаем запуск
      }
      else
      {
        // Второй экземпляр: посылаем команду "ACTIVATE" первому
        try
        {
          using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
          client.Connect(100); // 100 ms
          using var writer = new StreamWriter(client);
          writer.WriteLine("ACTIVATE");
          writer.Flush();
        }
        catch { /* ignore */ }

        return false; // НЕ продолжаем запуск
      }
    }

    private static void StartPipeServer()
    {
      ThreadPool.QueueUserWorkItem(_ =>
      {
        while (true)
        {
          try
          {
            using var server = new NamedPipeServerStream(
                PipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous
            );

            server.WaitForConnection();

            using var reader = new StreamReader(server);
            string? line = reader.ReadLine();
            if (line == "ACTIVATE")
            {
              ApplicationActivator.ActivateMainWindow();
            }
          }
          catch
          {
            // swallow
          }
        }
      });
    }
  }
}
