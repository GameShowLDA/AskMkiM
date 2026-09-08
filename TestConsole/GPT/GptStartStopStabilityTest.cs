using Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing;
using Ask.Device.Runtime.Device;

namespace TestConsole.GPT;

/// <summary>
/// Проверяет стабильность последовательных запусков и остановок GPT-79904.
/// </summary>
internal static class GptStartStopStabilityTest
{
  private const int CycleCount = 10;
  private const int StartDelayMs = 200;
  private const int StatusTimeoutMs = 1000;
  private const string StartCommand = "FUNC:TEST ON";
  private const string StatusCommand = "FUNC:TEST?";

  /// <summary>
  /// Выполняет десять циклов запуска, остановки и проверки состояния теста.
  /// </summary>
  internal static async Task RunAsync()
  {
    int baudRate = GPTAutoDiscoveryTest.ReadInt("Скорость", 115200);
    GPT79904? device = await GPTAutoDiscoveryTest.CreateDiscoveredDeviceAsync(baudRate);
    if (device == null)
    {
      Console.WriteLine("GPT79904 не найден ни на одном COM-порту.");
      return;
    }

    Console.WriteLine($"Для теста выбран GPT79904 на {device.COMPort.PortName}; база данных не используется.");

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("ВНИМАНИЕ: FUNC:TEST ON может подать высокое напряжение согласно текущей конфигурации ППУ.");
    Console.WriteLine("Перед запуском отключите испытуемое изделие и обеспечьте безопасное состояние стенда.");
    Console.ResetColor();
    Console.Write("Для запуска 10 циклов введите START: ");
    if (!string.Equals(Console.ReadLine()?.Trim(), "START", StringComparison.Ordinal))
    {
      Console.WriteLine("Тест отменён.");
      return;
    }

    if (!await GPT_Test.EnsureDeviceReadyAsync(device))
    {
      return;
    }

    var completedCycles = 0;
    try
    {
      for (var cycle = 1; cycle <= CycleCount; cycle++)
      {
        Console.WriteLine();
        Console.WriteLine($"Цикл {cycle}/{CycleCount}: запуск теста.");
        await device.DeviceProtocol.QueryAsync(StartCommand);
        await Task.Delay(StartDelayMs);

        string startedState = await device.DeviceProtocol.QueryAsync(
          StatusCommand,
          timeout: StatusTimeoutMs);
        Console.WriteLine($"Состояние после запуска: {FormatResponse(startedState)}");

        await device.IrManger.Measure.StopMeasure();

        string stoppedState = await device.DeviceProtocol.QueryAsync(
          StatusCommand,
          timeout: StatusTimeoutMs);
        bool isStopped = BreakdownTesterResponseProcessor.IsTestStopped(stoppedState);
        Console.WriteLine($"Состояние после остановки: {FormatResponse(stoppedState)}");

        if (!isStopped)
        {
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine($"Цикл {cycle}: ППУ не подтвердила TEST OFF. Последующие циклы отменены.");
          Console.ResetColor();
          break;
        }

        completedCycles++;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Цикл {cycle}: запуск и остановка подтверждены.");
        Console.ResetColor();
      }
    }
    catch (Exception ex)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine($"Аппаратный тест прерван: {ex.Message}");
      Console.ResetColor();
    }
    finally
    {
      try
      {
        await device.IrManger.Measure.StopMeasure();
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Не удалось выполнить финальную остановку ППУ: {ex.Message}");
        Console.ResetColor();
      }

      try
      {
        await device.ConnectableManager.DisconnectAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Не удалось закрыть соединение с ППУ: {ex.Message}");
      }
    }

    Console.WriteLine();
    Console.WriteLine($"Успешно завершено циклов: {completedCycles}/{CycleCount}.");
  }

  private static string FormatResponse(string response)
    => string.IsNullOrWhiteSpace(response) ? "ответ отсутствует" : response.Trim();
}
