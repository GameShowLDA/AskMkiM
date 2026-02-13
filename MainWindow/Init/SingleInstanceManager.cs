using System.IO;
using System.IO.Pipes;
using System.Text;

namespace MainWindowProgram.Init
{
  /// <summary>
  /// Отвечает за обеспечение единственного экземпляра приложения.
  /// </summary>
  internal static class SingleInstanceManager
  {
    private const string MutexName = "Global\\ASK-MKIM-M-SingleInstance";
    private const string PipeName = "ASK-MKIM-M-Pipe";
    private const string OpenFileCommandPrefix = "OPENFILE|";

    private static Mutex? _mutex;

    public static bool CheckOrSignal(string[]? args)
    {
      bool createdNew;
      _mutex = new Mutex(true, MutexName, out createdNew);

      if (createdNew)
      {
        // Первый экземпляр: запускаем слушатель.
        StartPipeServer();
        return true;
      }
      else
      {
        // Второй экземпляр: передаем команды первому и выходим.
        try
        {
          var filesToOpen = SupportedFileExtensions.ExtractSupportedExistingFiles(args);

          using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
          client.Connect(500);

          using var writer = new StreamWriter(client);
          writer.WriteLine("ACTIVATE");

          foreach (var filePath in filesToOpen)
          {
            var encodedPath = Convert.ToBase64String(Encoding.UTF8.GetBytes(filePath));
            writer.WriteLine($"{OpenFileCommandPrefix}{encodedPath}");
          }

          writer.Flush();
        }
        catch
        {
          // ignore
        }

        return false;
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
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
              ApplicationActivator.HandlePipeCommand(line);
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
