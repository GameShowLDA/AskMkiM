using System;
using System.IO.Ports;
using System.Management;

namespace TestConsole
{
  /// <summary>
  /// Класс <see cref="COMTest"/> предоставляет методы для работы с COM-портами.
  /// </summary>
  static internal class COMTest
  {
    /// <summary>
    /// Главный метод для управления COM-портами.
    /// </summary>
    public static void Run()
    {
      Console.WriteLine("=== Управление COM-портами ===");
      while (true)
      {
        // Получаем список доступных COM-портов
        string[] ports = GetAvailableComPorts();

        // Отображаем меню
        Console.WriteLine("\nДоступные COM-порты:");
        for (int i = 0; i < ports.Length; i++)
        {
          Console.WriteLine($"{i + 1}. {ports[i]}");
        }
        Console.WriteLine("0. Выход");

        // Запрашиваем выбор пользователя
        Console.Write("Выберите номер COM-порта или 0 для выхода: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > ports.Length)
        {
          Console.WriteLine("Неверный выбор. Попробуйте снова.");
          continue;
        }

        // Обработка выбора
        if (choice == 0)
        {
          Console.WriteLine("Выход из программы...");
          break;
        }

        // Получаем VID и PID выбранного порта
        string selectedPort = ports[choice - 1];
        var vidPid = GetVidPidForComPort(selectedPort);

        if (vidPid != null)
        {
          Console.WriteLine($"COM-порт: {selectedPort}");
          Console.WriteLine($"VID: {vidPid.Value.VID}, PID: {vidPid.Value.PID}");
        }
        else
        {
          Console.WriteLine($"Не удалось получить VID и PID для COM-порта: {selectedPort}");
        }
      }
    }

    /// <summary>
    /// Получает список доступных COM-портов.
    /// </summary>
    /// <returns>Массив строк с именами COM-портов.</returns>
    private static string[] GetAvailableComPorts()
    {
      return SerialPort.GetPortNames();
    }

    /// <summary>
    /// Получает VID и PID для указанного COM-порта.
    /// </summary>
    /// <param name="portName">Имя COM-порта.</param>
    /// <returns>Кортеж с VID и PID, если они найдены; иначе <c>null</c>.</returns>
    private static (string VID, string PID)? GetVidPidForComPort(string portName)
    {
      try
      {
        using (var searcher = new ManagementObjectSearcher(
            $"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%({portName})%'"))
        {
          foreach (var device in searcher.Get())
          {
            string deviceId = device["DeviceID"]?.ToString();
            if (!string.IsNullOrEmpty(deviceId))
            {
              // Извлекаем VID и PID из строки DeviceID
              var match = System.Text.RegularExpressions.Regex.Match(
                  deviceId,
                  @"VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})");

              if (match.Success)
              {
                string vid = match.Groups[1].Value;
                string pid = match.Groups[2].Value;
                return (vid, pid);
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка при получении VID и PID: {ex.Message}");
      }

      return null;
    }
  }
}