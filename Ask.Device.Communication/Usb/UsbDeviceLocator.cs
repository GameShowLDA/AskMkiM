using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Device.Communication.Usb
{
  /// <summary>
  /// Поиск USB-устройств в системе по шаблону имени.
  /// </summary>
  public static class UsbDeviceLocator
  {
    /// <summary>
    /// Ищет USB-устройство по имени.
    /// </summary>
    /// <param name="namePattern">Шаблон имени устройства (часть строки).</param>
    /// <param name="descriptor">Найденный дескриптор устройства.</param>
    /// <returns><c>true</c>, если устройство найдено; иначе <c>false</c>.</returns>
    public static bool TryFindByName(string namePattern, out UsbDeviceDescriptor descriptor)
    {
      descriptor = default;

      if (string.IsNullOrWhiteSpace(namePattern))
      {
        return false;
      }

      const string query = "SELECT Name, DeviceID, PNPDeviceID, Service FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'USB%' OR PNPDeviceID LIKE 'HID%'";

      using var searcher = new ManagementObjectSearcher(query);
      int bestScore = int.MinValue;

      foreach (ManagementObject item in searcher.Get())
      {
        string? name = item["Name"]?.ToString();
        string? deviceId = item["DeviceID"]?.ToString();
        string? pnpDeviceId = item["PNPDeviceID"]?.ToString();
        string? service = item["Service"]?.ToString();

        bool isMatch =
          (!string.IsNullOrWhiteSpace(name) &&
           name.IndexOf(namePattern, StringComparison.OrdinalIgnoreCase) >= 0) ||
          (!string.IsNullOrWhiteSpace(deviceId) &&
           deviceId.IndexOf(namePattern, StringComparison.OrdinalIgnoreCase) >= 0) ||
          (!string.IsNullOrWhiteSpace(pnpDeviceId) &&
           pnpDeviceId.IndexOf(namePattern, StringComparison.OrdinalIgnoreCase) >= 0);

        if (!isMatch)
        {
          continue;
        }

        int score = GetMatchScore(deviceId, pnpDeviceId, service);
        if (score <= bestScore)
        {
          continue;
        }

        bestScore = score;
        descriptor = new UsbDeviceDescriptor(
          BuildDisplayName(name, deviceId, pnpDeviceId),
          deviceId ?? string.Empty,
          pnpDeviceId ?? string.Empty,
          service ?? string.Empty);
      }

      return bestScore != int.MinValue;
    }

    private static int GetMatchScore(string? deviceId, string? pnpDeviceId, string? service)
    {
      int score = 0;

      if (!string.IsNullOrWhiteSpace(pnpDeviceId) &&
          pnpDeviceId.StartsWith("USB\\VID_", StringComparison.OrdinalIgnoreCase))
      {
        score += 100;
      }
      else if (!string.IsNullOrWhiteSpace(deviceId) &&
               deviceId.StartsWith("USB\\VID_", StringComparison.OrdinalIgnoreCase))
      {
        score += 90;
      }
      else if (!string.IsNullOrWhiteSpace(pnpDeviceId) &&
               pnpDeviceId.StartsWith("HID\\VID_", StringComparison.OrdinalIgnoreCase))
      {
        score += 80;
      }

      if (string.Equals(service, "HidUsb", StringComparison.OrdinalIgnoreCase))
      {
        score += 20;
      }

      return score;
    }

    private static string BuildDisplayName(string? name, string? deviceId, string? pnpDeviceId)
    {
      if (!string.IsNullOrWhiteSpace(name))
      {
        return name;
      }

      if (!string.IsNullOrWhiteSpace(deviceId))
      {
        return deviceId;
      }

      return pnpDeviceId ?? string.Empty;
    }
  }

  /// <summary>
  /// Описание USB-устройства, найденного в системе.
  /// </summary>
  /// <param name="Name">Имя устройства.</param>
  /// <param name="DeviceId">DeviceID в системе.</param>
  /// <param name="PnpDeviceId">PNPDeviceID в системе.</param>
  /// <param name="Service">Связанный драйверный сервис.</param>
  public readonly record struct UsbDeviceDescriptor(string Name, string DeviceId, string PnpDeviceId, string Service);
}
