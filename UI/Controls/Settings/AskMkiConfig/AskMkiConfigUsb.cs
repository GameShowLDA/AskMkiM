using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using Ivi.Visa;
using NationalInstruments.Visa;

namespace UI.Controls.Settings.AskMkiConfig;

/// <summary>
/// Поиск USB-подключения для runtime-мультиметров АСК.
/// </summary>
public partial class AskMkiConfigControl
{
  private static readonly Regex UsbVidPidRegex = new(@"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
  private static readonly Regex VisaVidPidRegex = new(@"USB\d*::0x([0-9A-F]{4})::0x([0-9A-F]{4})::", RegexOptions.IgnoreCase);
  private static readonly Regex UsbPortRegex = new(@"&0&(\d+)$", RegexOptions.IgnoreCase);

  /// <summary>
  /// Пытается найти USB-адрес выбранного мультиметра.
  /// </summary>
  private static string TryResolveUsbAddress(string deviceClass)
  {
    return TryResolveUsbInfo(deviceClass).Address;
  }

  /// <summary>
  /// Пытается найти USB-устройство выбранного мультиметра и вернуть данные для карточки настроек.
  /// </summary>
  private static UsbDeviceInfo TryResolveUsbInfo(string deviceClass)
  {
    var patterns = ResolveUsbSearchPatterns(deviceClass);
    if (patterns.Count == 0)
    {
      var visaInfo = TryResolveVisaUsbInfo(deviceClass);
      if (visaInfo != null)
      {
        return visaInfo;
      }

      return UsbDeviceInfo.NotFound("USB не найден");
    }

    try
    {
      foreach (var pattern in patterns)
      {
        var match = TryFindUsbDevice(pattern);
        if (match != null)
        {
          return BuildUsbInfo(match);
        }
      }

      var fallbackVisaInfo = TryResolveVisaUsbInfo(deviceClass);
      if (fallbackVisaInfo != null)
      {
        return fallbackVisaInfo;
      }
    }
    catch
    {
      var visaInfo = TryResolveVisaUsbInfo(deviceClass);
      if (visaInfo != null)
      {
        return visaInfo;
      }

      return UsbDeviceInfo.NotFound($"USB не найден: {patterns[0]}");
    }

    return UsbDeviceInfo.NotFound($"USB не найден: {patterns[0]}");
  }

  /// <summary>
  /// Заполняет карточку USB-устройства данными из уже сохраненного адреса.
  /// </summary>
  private static void FillUsbInfoFromAddress(AskMkiSettingItem item, string address)
  {
    if (string.IsNullOrWhiteSpace(address))
    {
      item.UsbStatus = "USB не найден";
      item.UsbPort = string.Empty;
      item.UsbId = string.Empty;
      item.UsbVid = "N/A";
      item.UsbPid = "N/A";
      return;
    }

    var match = UsbVidPidRegex.Match(address);
    item.UsbStatus = $"USB устройство: {address}";
    item.UsbPort = ExtractUsbPort(address);
    item.UsbId = address;
    item.UsbVid = match.Success ? match.Groups[1].Value.ToUpperInvariant() : "N/A";
    item.UsbPid = match.Success ? match.Groups[2].Value.ToUpperInvariant() : "N/A";
  }

  /// <summary>
  /// Формирует список признаков, по которым можно найти USB-устройство.
  /// </summary>
  private static List<string> ResolveUsbSearchPatterns(string deviceClass)
  {
    var patterns = new List<string>();
    var type = ResolveVoltmeterType(deviceClass);
    var device = CreateVoltmeterDevice(deviceClass);
    if (type == null || !IsUsbVoltmeter(deviceClass) || device == null)
    {
      return patterns;
    }

    AddUsbSearchPattern(patterns, device.ConnectionDetails);
    AddUsbSearchPattern(patterns, device.Name);
    AddUsbSearchPattern(patterns, type.Name);

    return patterns;
  }

  /// <summary>
  /// Ищет USB-устройство по одному признаку.
  /// </summary>
  private static UsbDeviceMatch? TryFindUsbDevice(string pattern)
  {
    const string query = "SELECT Name, DeviceID, PNPDeviceID, Service FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'USB%' OR PNPDeviceID LIKE 'HID%'";

    using var searcher = new ManagementObjectSearcher(query);
    UsbDeviceMatch? bestMatch = null;
    var bestScore = int.MinValue;

    foreach (ManagementObject item in searcher.Get())
    {
      var name = item["Name"]?.ToString() ?? string.Empty;
      var deviceId = item["DeviceID"]?.ToString() ?? string.Empty;
      var pnpDeviceId = item["PNPDeviceID"]?.ToString() ?? string.Empty;
      var service = item["Service"]?.ToString() ?? string.Empty;

      if (!name.Contains(pattern, StringComparison.OrdinalIgnoreCase)
          && !deviceId.Contains(pattern, StringComparison.OrdinalIgnoreCase)
          && !pnpDeviceId.Contains(pattern, StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      var score = GetUsbMatchScore(deviceId, pnpDeviceId, service);
      if (score <= bestScore)
      {
        continue;
      }

      var source = string.IsNullOrWhiteSpace(pnpDeviceId) ? deviceId : pnpDeviceId;
      var match = UsbVidPidRegex.Match(source);
      var vid = match.Success ? match.Groups[1].Value : string.Empty;
      var pid = match.Success ? match.Groups[2].Value : string.Empty;

      bestScore = score;
      bestMatch = new UsbDeviceMatch(deviceId, pnpDeviceId, vid, pid);
    }

    return bestMatch;
  }

  /// <summary>
  /// Добавляет непустой признак поиска USB-устройства.
  /// </summary>
  private static void AddUsbSearchPattern(List<string> patterns, string? pattern)
  {
    if (string.IsNullOrWhiteSpace(pattern))
    {
      return;
    }

    if (patterns.Any(item => string.Equals(item, pattern, StringComparison.OrdinalIgnoreCase)))
    {
      return;
    }

    patterns.Add(pattern);
  }

  /// <summary>
  /// Оценивает качество совпадения найденного USB-устройства.
  /// </summary>
  private static int GetUsbMatchScore(string deviceId, string pnpDeviceId, string service)
  {
    var score = 0;

    if (pnpDeviceId.StartsWith("USB\\VID_", StringComparison.OrdinalIgnoreCase))
    {
      score += 100;
    }
    else if (deviceId.StartsWith("USB\\VID_", StringComparison.OrdinalIgnoreCase))
    {
      score += 90;
    }
    else if (pnpDeviceId.StartsWith("HID\\VID_", StringComparison.OrdinalIgnoreCase))
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
  /// Формирует строку USB-адреса для сохранения в конфигурации.
  /// </summary>
  private static string BuildUsbAddress(UsbDeviceMatch match)
  {
    if (!string.IsNullOrWhiteSpace(match.Vid) && !string.IsNullOrWhiteSpace(match.Pid))
    {
      return $"VID_{match.Vid.ToUpperInvariant()}&PID_{match.Pid.ToUpperInvariant()}";
    }

    if (!string.IsNullOrWhiteSpace(match.PnpDeviceId))
    {
      return match.PnpDeviceId;
    }

    return match.DeviceId;
  }

  /// <summary>
  /// Формирует полное описание найденного USB-устройства для отображения и сохранения.
  /// </summary>
  private static UsbDeviceInfo BuildUsbInfo(UsbDeviceMatch match)
  {
    var address = BuildUsbAddress(match);
    var source = string.IsNullOrWhiteSpace(match.PnpDeviceId) ? match.DeviceId : match.PnpDeviceId;
    var port = ExtractUsbPort(source);
    var vid = string.IsNullOrWhiteSpace(match.Vid) ? "N/A" : match.Vid.ToUpperInvariant();
    var pid = string.IsNullOrWhiteSpace(match.Pid) ? "N/A" : match.Pid.ToUpperInvariant();

    return new UsbDeviceInfo(
      address,
      "USB устройство найдено",
      port,
      source,
      vid,
      pid);
  }

  /// <summary>
  /// Извлекает номер USB-порта из аппаратного идентификатора Windows.
  /// </summary>
  private static string ExtractUsbPort(string source)
  {
    var match = UsbPortRegex.Match(source);
    return match.Success ? match.Groups[1].Value : string.Empty;
  }

  private static UsbDeviceInfo? TryResolveVisaUsbInfo(string deviceClass)
  {
    var identityPatterns = ResolveVisaIdentityPatterns(deviceClass);
    if (identityPatterns.Count == 0)
    {
      return null;
    }

    try
    {
      using var resourceManager = new ResourceManager();
      var resources = resourceManager.Find("USB?*INSTR").ToArray();
      foreach (var resource in resources)
      {
        var identity = TryReadVisaIdentity(resourceManager, resource);
        if (identity == null)
        {
          continue;
        }

        if (!IsVisaIdentityMatch(identity, identityPatterns))
        {
          continue;
        }

        return BuildVisaUsbInfo(resource, identity);
      }
    }
    catch (Exception ex) when (ex is VisaException or NativeVisaException or InvalidOperationException)
    {
      return null;
    }

    return null;
  }

  private static List<string> ResolveVisaIdentityPatterns(string deviceClass)
  {
    var patterns = new List<string>();
    var device = CreateVoltmeterDevice(deviceClass);
    if (device == null)
    {
      return patterns;
    }

    AddVisaIdentityPatterns(patterns, device.Name);
    AddVisaIdentityPatterns(patterns, device.Description);
    AddVisaIdentityPatterns(patterns, deviceClass);
    return patterns;
  }

  private static void AddVisaIdentityPatterns(List<string> patterns, string? source)
  {
    if (string.IsNullOrWhiteSpace(source))
    {
      return;
    }

    foreach (Match match in Regex.Matches(source, @"[\p{L}\d][\p{L}\d\-/\.]*"))
    {
      var pattern = NormalizeVisaIdentity(match.Value);
      if (pattern.Length < 3 || !pattern.Any(char.IsDigit))
      {
        continue;
      }

      if (!patterns.Contains(pattern, StringComparer.OrdinalIgnoreCase))
      {
        patterns.Add(pattern);
      }
    }
  }

  private static bool IsVisaIdentityMatch(string identity, List<string> patterns)
  {
    var normalizedIdentity = NormalizeVisaIdentity(identity);
    return patterns.Any(pattern => normalizedIdentity.Contains(pattern, StringComparison.OrdinalIgnoreCase));
  }

  private static string NormalizeVisaIdentity(string value)
  {
    return new string(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
  }

  private static string? TryReadVisaIdentity(ResourceManager resourceManager, string resource)
  {
    try
    {
      using var session = resourceManager.Open(resource);
      if (session is not MessageBasedSession messageSession)
      {
        return null;
      }

      messageSession.TimeoutMilliseconds = 2000;
      messageSession.SendEndEnabled = true;
      messageSession.TerminationCharacter = (byte)'\n';
      messageSession.TerminationCharacterEnabled = true;
      messageSession.RawIO.Write("*IDN?\n");

      byte[] buffer = new byte[4096];
      messageSession.RawIO.Read(buffer, 0, buffer.Length, out long readCount, out _);
      return readCount <= 0
        ? null
        : Encoding.ASCII.GetString(buffer, 0, (int)readCount).Trim('\0', '\r', '\n', ' ');
    }
    catch (Exception ex) when (ex is VisaException or NativeVisaException or IOTimeoutException or InvalidOperationException)
    {
      return null;
    }
  }

  private static UsbDeviceInfo BuildVisaUsbInfo(string resource, string identity)
  {
    var match = VisaVidPidRegex.Match(resource);
    var vid = match.Success ? match.Groups[1].Value.ToUpperInvariant() : "N/A";
    var pid = match.Success ? match.Groups[2].Value.ToUpperInvariant() : "N/A";

    return new UsbDeviceInfo(
      resource,
      $"USB VISA устройство найдено: {identity}",
      string.Empty,
      identity,
      vid,
      pid);
  }

  /// <summary>
  /// Обновляет карточку USB-устройства в редакторе.
  /// </summary>
  private static void ApplyUsbInfo(AskMkiSettingItem item, UsbDeviceInfo info)
  {
    item.TextValue = info.Address;
    item.UsbStatus = info.Status;
    item.UsbPort = info.Port;
    item.UsbId = info.DeviceId;
    item.UsbVid = info.Vid;
    item.UsbPid = info.Pid;
  }

  private sealed record UsbDeviceMatch(string DeviceId, string PnpDeviceId, string Vid, string Pid);

  private sealed record UsbDeviceInfo(
    string Address,
    string Status,
    string Port,
    string DeviceId,
    string Vid,
    string Pid)
  {
    public static UsbDeviceInfo NotFound(string status)
    {
      return new UsbDeviceInfo(string.Empty, status, string.Empty, string.Empty, "N/A", "N/A");
    }
  }
}
