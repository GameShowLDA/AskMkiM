using NationalInstruments.Visa;

namespace Ask.Device.Runtime.Function.B7783;

/// <summary>
/// Выполняет поиск USBTMC/VISA-ресурсов измерительных приборов.
/// </summary>
public static class UsbTmcInstrumentLocator
{
  private const string UsbTmcResourcePattern = "USB?*INSTR";

  /// <summary>
  /// Пытается найти VISA-ресурс USB-прибора по шаблону или возвращает единственный найденный USBTMC-ресурс.
  /// </summary>
  /// <param name="pattern">Шаблон поиска ресурса: часть VISA-адреса, VID/PID или пустая строка.</param>
  /// <param name="resourceName">Найденный VISA-ресурс.</param>
  /// <param name="error">Описание ошибки поиска.</param>
  /// <returns><see langword="true"/>, если ресурс найден; иначе <see langword="false"/>.</returns>
  public static bool TryFindInstrumentResource(string? pattern, out string resourceName, out string error)
  {
    resourceName = string.Empty;
    error = string.Empty;

    try
    {
      using var resourceManager = new ResourceManager();
      var resources = resourceManager.Find(UsbTmcResourcePattern).ToList();

      var matched = resources.FirstOrDefault(resource => IsResourceMatch(resource, pattern));
      if (!string.IsNullOrWhiteSpace(matched))
      {
        resourceName = matched;
        return true;
      }

      if (resources.Count == 1)
      {
        resourceName = resources[0];
        return true;
      }

      error = resources.Count == 0
        ? "USB-приборы VISA/USBTMC не найдены."
        : $"Найдено несколько USBTMC-ресурсов: {string.Join(", ", resources)}.";

      return false;
    }
    catch (Exception ex)
    {
      error = $"Ошибка поиска USBTMC-ресурса: {ex.Message}";
      return false;
    }
  }

  /// <summary>
  /// Проверяет, соответствует ли VISA-ресурс указанному шаблону.
  /// </summary>
  /// <param name="resource">VISA-ресурс.</param>
  /// <param name="pattern">Шаблон поиска.</param>
  /// <returns><see langword="true"/>, если ресурс соответствует шаблону.</returns>
  private static bool IsResourceMatch(string resource, string? pattern)
  {
    if (string.IsNullOrWhiteSpace(pattern))
    {
      return true;
    }

    if (resource.Contains(pattern, StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    var match = System.Text.RegularExpressions.Regex.Match(
      pattern,
      @"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    if (!match.Success)
    {
      return false;
    }

    var vid = $"0x{match.Groups[1].Value}";
    var pid = $"0x{match.Groups[2].Value}";
    return resource.Contains(vid, StringComparison.OrdinalIgnoreCase) &&
           resource.Contains(pid, StringComparison.OrdinalIgnoreCase);
  }
}
