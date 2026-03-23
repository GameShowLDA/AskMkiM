using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Diagnostics;
using System.IO.Ports;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Com.Extensions
{
  /// <summary>
  /// Содержит расширения для работы с <see cref="SerialPort"/>.
  /// </summary>
  public static class SerialPortExtensions
  {
    /// <summary>
    /// Открывает COM-порт и возвращает объект-заглушку для использования в конструкции <c>using</c>.
    /// </summary>
    /// <param name="port">Порт, который требуется открыть.</param>
    /// <param name="deviceName">Имя устройства для логов и пользовательских сообщений.</param>
    /// <param name="userMessageService">Сервис показа пользовательских сообщений.</param>
    /// <returns>Объект-заглушка, совместимый с паттерном <c>using</c>.</returns>
    public static async Task<IDisposable> UsePort(
      this SerialPort port,
      string? deviceName = null,
      IUserInteractionService? userMessageService = null)
    {
      ArgumentNullException.ThrowIfNull(port);

      if (port.IsOpen)
      {
        LogWarning($"[{deviceName ?? "Unknown"}] COM-порт {port.PortName} уже открыт другим местом.", isDeviceLog: true);
        return new NoOpDisposable();
      }

      const int maxAttempts = 100;
      const int retryDelay = 1000;
      int attempt = 0;

      while (true)
      {
        userMessageService?.GetCancellationToken().ThrowIfCancellationRequested();

        attempt++;
        var header = $"[{deviceName ?? "Unknown"}]";

        try
        {
          port.Open();
          LogDebug($"{header} COM-порт {port.PortName} открыт (попытка {attempt}).", isDeviceLog: true);

          await Task.Delay(100);
          return new NoOpDisposable();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("The port is already open"))
        {
          var message = $"COM-порт {port.PortName} уже был открыт (попытка {attempt}).";
          LogWarning($"{header} {message}", isDeviceLog: true);
          await ShowUserMessageAsync(userMessageService, header, message);
          return new NoOpDisposable();
        }
        catch (UnauthorizedAccessException ex)
        {
          var message = $"Попытка {attempt}/{maxAttempts}: порт {port.PortName} занят ({ex.Message}).";
          LogWarning($"{header} {message}", isDeviceLog: true);
          await ShowUserMessageAsync(userMessageService, header, message);

          if (attempt >= maxAttempts)
          {
            message = $"COM-порт {port.PortName} не удалось открыть после {maxAttempts} попыток.";
            await ShowUserMessageAsync(userMessageService, header, message);
            LogException($"{header} {message}", ex, isDeviceLog: true);
            throw;
          }

          await Task.Delay(retryDelay);
        }
        catch (IOException ex)
        {
          var message = $"Попытка {attempt}/{maxAttempts}: ошибка I/O при открытии {port.PortName} ({ex.Message}).";
          LogWarning($"{header} {message}", isDeviceLog: true);
          await ShowUserMessageAsync(userMessageService, header, message);

          if (attempt >= maxAttempts)
          {
            message = $"COM-порт {port.PortName} не удалось открыть после {maxAttempts} попыток.";
            LogException($"{header} {message}", ex, isDeviceLog: true);
            await ShowUserMessageAsync(userMessageService, header, message);
            throw;
          }

          await Task.Delay(retryDelay);
        }
        catch (Exception ex)
        {
          var message = $"Попытка {attempt}/{maxAttempts}: общая ошибка при открытии {port.PortName} ({ex.Message}).";
          LogWarning($"{header} {message}", isDeviceLog: true);
          await ShowUserMessageAsync(userMessageService, header, message);

          if (attempt >= maxAttempts)
          {
            message = $"COM-порт {port.PortName} не удалось открыть после {maxAttempts} попыток.";
            LogException($"{header} {message}", ex, isDeviceLog: true);
            await ShowUserMessageAsync(userMessageService, header, message);
            throw;
          }

          await Task.Delay(retryDelay);
        }
      }
    }

    /// <summary>
    /// Возвращает отчёт утилиты <c>handle.exe</c> о процессах, удерживающих указанный COM-порт.
    /// </summary>
    /// <param name="comPort">Имя COM-порта.</param>
    /// <returns>Текстовый отчёт утилиты <c>handle.exe</c>.</returns>
    public static string GetComPortUsageReport(string comPort)
    {
      var processStartInfo = new ProcessStartInfo
      {
        FileName = "handle.exe",
        Arguments = comPort,
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      };

      using var process = Process.Start(processStartInfo);
      if (process == null)
      {
        return string.Empty;
      }

      string output = process.StandardOutput.ReadToEnd();
      process.WaitForExit();
      return output;
    }

    /// <summary>
    /// Показывает пользователю сообщение об ошибке работы с COM-портом.
    /// </summary>
    /// <param name="userMessageService">Сервис пользовательских сообщений.</param>
    /// <param name="header">Заголовок сообщения.</param>
    /// <param name="message">Текст сообщения.</param>
    private static async Task ShowUserMessageAsync(IUserInteractionService? userMessageService, string header, string message)
    {
      if (userMessageService == null)
      {
        return;
      }

      await userMessageService.ShowMessageAsync(
        new ShowMessageModel(header, message: message, type: ShowMessageModel.MessageType.Error)
        {
          IndentLevel = 2,
        });
    }

    /// <summary>
    /// Представляет пустой объект <see cref="IDisposable"/>, совместимый с конструкцией <c>using</c>.
    /// </summary>
    private sealed class NoOpDisposable : IDisposable
    {
      /// <summary>
      /// Освобождает заглушку. Реального действия не выполняет.
      /// </summary>
      public void Dispose()
      {
      }
    }
  }
}
