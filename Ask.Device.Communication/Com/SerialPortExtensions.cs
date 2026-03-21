using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Diagnostics;
using System.IO.Ports;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Com
{
  /// <summary>
  /// Расширения для управления SerialPort.
  /// </summary>
  public static class SerialPortExtensions
  {
    /// <summary>
    /// Открывает порт (если он ещё не открыт).
    /// Делает до 5 попыток с задержкой, если порт занят.
    /// Возвращает IDisposable-заглушку для совместимости с using.
    /// </summary>
    public static async Task<IDisposable> UsePort(
        this SerialPort port,
        string deviceName = null,
        IUserInteractionService userMessageService = null)
    {
      if (port == null)
        throw new ArgumentNullException(nameof(port));

      if (port.IsOpen)
      {
        LogWarning($"[{deviceName ?? "Unknown"}] COM-порт {port.PortName} уже открыт другим местом.", isDeviceLog: true);
        return new DummyReleaser();
      }

      const int maxAttempts = 100;
      const int retryDelay = 1000; // мс между попытками
      int attempt = 0;

      while (true)
      {
        if (userMessageService != null)
        {
          userMessageService.GetCancellationToken().ThrowIfCancellationRequested();
        }

        attempt++;
        var header = $"[{deviceName ?? "Unknown"}]";

        try
        {
          port.Open();
          LogDebug($"{header} COM-порт {port.PortName} открыт (попытка {attempt}).", isDeviceLog: true);

          await Task.Delay(100);
          return new DummyReleaser();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("The port is already open"))
        {
          var str = $"COM-порт {port.PortName} уже был открыт (попытка {attempt}).";
          LogWarning($"{header} {str}", isDeviceLog: true);
          await userMessageService.ShowMessageAsync(new ShowMessageModel(header, message: str, type: ShowMessageModel.MessageType.Error) { IndentLevel = 2 });
          return new DummyReleaser();
        }
        catch (UnauthorizedAccessException ex)
        {
          var str = $"Попытка {attempt}/{maxAttempts}: порт {port.PortName} занят ({ex.Message}).";
          LogWarning($"{header} {str}", isDeviceLog: true);

          if (userMessageService != null)
          {
            await userMessageService.ShowMessageAsync(new ShowMessageModel(header, message: str, type: ShowMessageModel.MessageType.Error) { IndentLevel = 2 });
          }

          if (attempt >= maxAttempts)
          {
            str = $"COM-порт {port.PortName} не удалось открыть после {maxAttempts} попыток.";

            if (userMessageService != null)
            {
              await userMessageService.ShowMessageAsync(new ShowMessageModel(header, message: str, type: ShowMessageModel.MessageType.Error) { IndentLevel = 2 });
            }

            LogException($"{header} {str}", ex, isDeviceLog: true);
            throw;
          }

          await Task.Delay(retryDelay);
        }

        catch (IOException ex)
        {
          var str = $"Попытка {attempt}/{maxAttempts}: ошибка I/O при открытии {port.PortName} ({ex.Message}).";
          LogWarning($"{header} {str}", isDeviceLog: true);
          if (userMessageService != null)
          {
            await userMessageService.ShowMessageAsync(new ShowMessageModel(header, message: str, type: ShowMessageModel.MessageType.Error) { IndentLevel = 2 });
          }

          if (attempt >= maxAttempts)
          {
            str = $"COM-порт {port.PortName} не удалось открыть после {maxAttempts} попыток.";
            LogException($"{header} {str}", ex, isDeviceLog: true);
            await userMessageService.ShowMessageAsync(new ShowMessageModel(header, message: str, type: ShowMessageModel.MessageType.Error) { IndentLevel = 2 });
            throw;
          }

          await Task.Delay(retryDelay);
        }
        catch (Exception ex)
        {
          var str = $"Попытка {attempt}/{maxAttempts}: общая ошибка при открытии {port.PortName} ({ex.Message}).";
          LogWarning($"{header} {str}", isDeviceLog: true);
          await userMessageService.ShowMessageAsync(new ShowMessageModel(header, message: str, type: ShowMessageModel.MessageType.Error) { IndentLevel = 2 });

          if (attempt >= maxAttempts)
          {
            str = $"COM-порт {port.PortName} не удалось открыть после {maxAttempts} попыток.";
            LogException($"{header} {str}", ex, isDeviceLog: true);
            await userMessageService.ShowMessageAsync(new ShowMessageModel(header, message: str, type: ShowMessageModel.MessageType.Error) { IndentLevel = 2 });
            throw;
          }

          await Task.Delay(retryDelay);
        }
      }
    }

    public static string WhoUsesCom(string comPort)
    {
      var psi = new ProcessStartInfo
      {
        FileName = "handle.exe",
        Arguments = comPort,
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };
      using var process = Process.Start(psi);
      string output = process.StandardOutput.ReadToEnd();
      process.WaitForExit();
      return output;
    }

    /// <summary>
    /// Заглушка для using. Ничего не делает при Dispose.
    /// </summary>
    private sealed class DummyReleaser : IDisposable
    {
      public void Dispose() { }
    }
  }
}
