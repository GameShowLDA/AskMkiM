using System.Globalization;
using System.Net.Sockets;

namespace TestConsole.B5108;

internal static partial class B5108Probe
{
  private sealed record ParameterRange(decimal Minimum, decimal Maximum, decimal Step);

  private static async Task<decimal> ReadNumberAsync(NetworkStream stream, string command)
    => decimal.Parse(await ExchangeAsync(stream, command), NumberStyles.Float, CultureInfo.InvariantCulture);

  private static string Number(decimal value) => value.ToString("0.#######", CultureInfo.InvariantCulture);

  private static async Task<decimal> SetAndReadAsync(NetworkStream stream, string command, decimal value, int displayDelayMs = 0)
  {
    await RequireOutputOffAsync(stream);
    await ExchangeAsync(stream, $"{command} {Number(value)}");
    await RequireNoErrorAsync(stream);
    decimal readback = await ReadNumberAsync(stream, command + "?");
    if (displayDelayMs > 0)
    {
      Console.WriteLine($"{(command == "VOLT" ? "Напряжение" : "Ток")}: задано {Number(value)}, прочитано {Number(readback)}. Пауза 1,5 с.");
      await Task.Delay(displayDelayMs);
    }
    return readback;
  }

  private static async Task RequireNoErrorAsync(NetworkStream stream)
  {
    string error = await ExchangeAsync(stream, "SYST:ERR?");
    if (error != "0" && !error.StartsWith("0,"))
      throw new InvalidOperationException($"Ошибка прибора: {error}");
  }

  private static async Task RequireOutputOffAsync(NetworkStream stream)
  {
    if (await ReadNumberAsync(stream, "OUTP?") != 0)
      throw new InvalidOperationException("Для определения параметров выход должен быть выключен.");
  }

  private static async Task<ParameterRange> FindRangeAsync(NetworkStream stream, string command, string title, string unit, int displayDelayMs)
  {
    decimal limit = await ReadNumberAsync(stream, command + ":LIM?");
    Console.WriteLine($"\n=== {title}: минимум, максимум, шаг. Выход выключен ===");
    decimal minimum = await SetAndReadAsync(stream, command, 0, displayDelayMs);
    decimal maximum = await SetAndReadAsync(stream, command, limit, displayDelayMs);
    if (maximum <= minimum)
      throw new InvalidOperationException($"Не удалось подтвердить диапазон {command}.");
    decimal step = await FindStepAsync(stream, command, minimum, maximum, displayDelayMs);
    Console.WriteLine($"\n{title}\n1. Наблюдаемый шаг SCPI-уставки у минимума: {Number(step)} {unit}" +
      $"\n2. Минимальное: {Number(minimum)} {unit}\n3. Максимальное при текущем LIM: {Number(maximum)} {unit}");
    foreach (decimal anchor in new[] { maximum / 2, maximum * 0.99m })
    {
      decimal baseline = await SetAndReadAsync(stream, command, anchor, displayDelayMs);
      decimal localStep = await FindStepAsync(stream, command, baseline, maximum, displayDelayMs);
      Console.WriteLine($"Шаг около {Number(baseline)} {unit}: {Number(localStep)} {unit}");
    }
    await SetAndReadAsync(stream, command, minimum);
    return new ParameterRange(minimum, maximum, step);
  }

  private static async Task<decimal> FindStepAsync(NetworkStream stream, string command, decimal baseline, decimal maximum, int displayDelayMs)
  {
    await SetAndReadAsync(stream, command, baseline);
    for (decimal increment = 0.0000001m; increment <= maximum - baseline; increment *= 10)
    {
      decimal readback = await SetAndReadAsync(stream, command, baseline + increment, displayDelayMs);
      if (readback > baseline) return readback - baseline;
    }
    throw new InvalidOperationException($"Шаг {command} не найден от 0.0000001 в исследованной точке.");
  }

  private static async Task CharacterizeAsync(NetworkStream stream, bool outputTest, int displayDelayMs)
  {
    await RequireOutputOffAsync(stream);
    decimal originalVoltage = await ReadNumberAsync(stream, "VOLT?");
    decimal originalCurrent = await ReadNumberAsync(stream, "CURR?");
    try
    {
      await SetAndReadAsync(stream, "VOLT", 0);
      await SetAndReadAsync(stream, "CURR", 0);
      ParameterRange voltage = await FindRangeAsync(stream, "VOLT", "Напряжение", "В", displayDelayMs);
      ParameterRange current = await FindRangeAsync(stream, "CURR", "Сила тока", "А", displayDelayMs);
      if (outputTest)
      {
        decimal testVoltage = voltage.Minimum > 0 ? voltage.Minimum : voltage.Step;
        decimal testCurrent = current.Minimum > 0 ? current.Minimum : current.Step;
        if (testVoltage > 0.01m || testCurrent > 0.01m)
          throw new InvalidOperationException("Минимальные уставки превышают границы проверки 0.01 В / 0.01 А.");
        decimal actualVoltage = await SetAndReadAsync(stream, "VOLT", testVoltage);
        decimal actualCurrent = await SetAndReadAsync(stream, "CURR", testCurrent);
        if (actualVoltage != testVoltage || actualCurrent != testCurrent)
          throw new InvalidOperationException("Прибор изменил тестовые уставки.");
        try
        {
          await ExchangeAsync(stream, "OUTP ON");
          await RequireNoErrorAsync(stream);
          if (await ReadNumberAsync(stream, "OUTP?") != 1)
            throw new InvalidOperationException("Выход не включился.");
          await Task.Delay(Math.Max(500, displayDelayMs));
          await ExchangeAsync(stream, "MEAS:VOLT?");
          await ExchangeAsync(stream, "MEAS:CURR?");
        }
        finally
        {
          await ExchangeAsync(stream, "OUTP OFF");
          await RequireOutputOffAsync(stream);
        }
        Console.WriteLine("Проверка включения завершена, выход выключен.");
      }
    }
    finally
    {
      await RequireOutputOffAsync(stream);
      await SetAndReadAsync(stream, "VOLT", 0);
      await SetAndReadAsync(stream, "CURR", originalCurrent);
      await SetAndReadAsync(stream, "VOLT", originalVoltage);
      Console.WriteLine("Исходные уставки восстановлены; *SAV не выполнялась.");
    }
  }
}
