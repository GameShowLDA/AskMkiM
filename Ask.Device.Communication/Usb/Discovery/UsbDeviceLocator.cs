using System.Management;

namespace Ask.Device.Communication.Usb
{
  /// <summary>
  /// Выполняет поиск USB-устройств в системе по имени или идентификатору.
  /// </summary>
  public static class UsbDeviceLocator
  {
    /// <summary>
    /// Ищет USB-устройство по шаблону имени.
    /// </summary>
    /// <param name="namePattern">Шаблон имени устройства или его идентификатора.</param>
    /// <param name="descriptor">Дескриптор найденного устройства.</param>
    /// <returns><see langword="true"/>, если устройство найдено; иначе <see langword="false"/>.</returns>
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

    /// <summary>
    /// Вычисляет приоритет совпадения для найденного USB-устройства.
    /// </summary>
    /// <param name="deviceId">DeviceID устройства.</param>
    /// <param name="pnpDeviceId">PnP DeviceID устройства.</param>
    /// <param name="service">Имя драйверного сервиса.</param>
    /// <returns>Числовой вес совпадения.</returns>
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

    /// <summary>
    /// Формирует отображаемое имя USB-устройства.
    /// </summary>
    /// <param name="name">Человекочитаемое имя устройства.</param>
    /// <param name="deviceId">DeviceID устройства.</param>
    /// <param name="pnpDeviceId">PnP DeviceID устройства.</param>
    /// <returns>Лучшая доступная строка для отображения устройства.</returns>
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
  /// Описывает USB-устройство, найденное в системе.
  /// </summary>
  public readonly record struct UsbDeviceDescriptor
  {
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UsbDeviceDescriptor"/>.
    /// </summary>
    /// <param name="name">Имя устройства.</param>
    /// <param name="deviceId">Системный DeviceID.</param>
    /// <param name="pnpDeviceId">Системный PnP DeviceID.</param>
    /// <param name="service">Имя драйверного сервиса.</param>
    public UsbDeviceDescriptor(string name, string deviceId, string pnpDeviceId, string service)
    {
      Name = name;
      DeviceId = deviceId;
      PnpDeviceId = pnpDeviceId;
      Service = service;
    }

    /// <summary>
    /// Получает имя устройства.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Получает системный идентификатор DeviceID.
    /// </summary>
    public string DeviceId { get; init; }

    /// <summary>
    /// Получает системный идентификатор PnP DeviceID.
    /// </summary>
    public string PnpDeviceId { get; init; }

    /// <summary>
    /// Получает имя драйверного сервиса устройства.
    /// </summary>
    public string Service { get; init; }
  }
}
