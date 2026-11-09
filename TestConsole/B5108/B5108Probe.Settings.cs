using System.Globalization;
using System.Net.Sockets;

namespace TestConsole.B5108;

internal static partial class B5108Probe
{
  private const decimal VoltageStep = 0.1m;
  private const decimal CurrentStep = 0.001m;

  private static async Task RunSettingsAsync(NetworkStream stream)
  {
    await RequireOutputOffAsync(stream);
    while (true)
    {
      decimal voltage = await ReadNumberAsync(stream, "VOLT?");
      decimal current = await ReadNumberAsync(stream, "CURR?");
      Console.WriteLine($"\nУставки: {Number(voltage)} В / {Number(current)} А.");
      Console.WriteLine("Программный шаг: 0,1 В / 0,001 А (1 мА).");
      Console.WriteLine("1. Установить напряжение\n2. Установить ток\n0. Назад");
      string? choice = Console.ReadLine()?.Trim();
      if (choice is null or "0") return;
      if (choice is not ("1" or "2"))
      {
        Console.WriteLine("Выберите пункт 0–2.");
        continue;
      }
      string command = choice == "1" ? "VOLT" : "CURR";
      string unit = choice == "1" ? "В" : "А";
      decimal step = choice == "1" ? VoltageStep : CurrentStep;
      decimal limit = await ReadNumberAsync(stream, command + ":LIM?");
      Console.Write($"Введите значение в {unit} (0–{Number(limit)}, шаг {Number(step)}; пустая строка — назад): ");
      string? input = Console.ReadLine();
      if (string.IsNullOrWhiteSpace(input)) continue;
      if (!decimal.TryParse(input.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out decimal value)
          || value < 0 || value > limit || value % step != 0)
      {
        Console.WriteLine($"Недопустимое значение: требуется число от 0 до {Number(limit)} {unit}, кратное {Number(step)} {unit}.");
        continue;
      }
      decimal actual = await SetAndReadAsync(stream, command, value);
      if (actual != value)
        throw new InvalidOperationException($"Прибор изменил уставку: отправлено {Number(value)}, прочитано {Number(actual)} {unit}.");
      Console.WriteLine($"Установлено и подтверждено: {Number(actual)} {unit}. Выход выключен.");
    }
  }
}
