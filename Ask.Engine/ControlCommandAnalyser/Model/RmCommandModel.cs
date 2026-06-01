using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  /// <summary>
  /// Модель для команды РМ: хранит сопоставление точек.
  /// </summary>
  public class RmCommandModel : BaseCommandModel
  {
    public override string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.RM).DisplayName;

    /// <summary>
    /// Словарь: "точка источника" → "точка назначения".
    /// </summary>
    public Dictionary<string, string> PointsMap { get; set; } = new();

    public Dictionary<string, string> SynonymMap { get; set; } = new();

    public List<RmPartModel> Parts { get; set; } = new();

    public List<RmPairModel> Pairs { get; set; } = new();

    public IEnumerable<string> ToExpandedLines()
    {
      foreach (var kv in PointsMap)
        yield return $"{kv.Key} => {kv.Value}";
    }

    public List<string> GetAllDestinationPoints()
    {
      return PointsMap.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Получает ключ по точке.
    /// </summary>
    /// <param name="value">Точка.</param>
    /// <param name="key">Ключ.</param>
    /// <returns></returns>
    public bool TryGetKeyByValue(string value, out string key)
    {
      key = null;

      if (!PointsMap.ContainsValue(value))
      {
        return false;
      }

      foreach (var kv in PointsMap)
      {
        if (kv.Value.Equals(value))
        {
          key = kv.Key;
          break;
        }
      }

      return true;
    }

    /// <summary>
    /// Ищет адрес точки по её мнемонике.
    /// Сначала выполняет точный поиск, затем — поиск по нормализованному ключу,
    /// который сглаживает типичные кириллические/латинские визуальные дубликаты.
    /// </summary>
    public bool TryGetAddressByKey(string pointKey, out string address)
    {
      address = string.Empty;

      if (string.IsNullOrWhiteSpace(pointKey))
        return false;

      if (PointsMap.TryGetValue(pointKey, out address))
        return true;

      if (SynonymMap.TryGetValue(pointKey, out address))
        return true;

      string normalizedKey = NormalizePointKey(pointKey);
      foreach (var kv in PointsMap)
      {
        if (string.Equals(NormalizePointKey(kv.Key), normalizedKey, StringComparison.OrdinalIgnoreCase))
        {
          address = kv.Value;
          return true;
        }
      }

      foreach (var kv in SynonymMap)
      {
        if (string.Equals(NormalizePointKey(kv.Key), normalizedKey, StringComparison.OrdinalIgnoreCase))
        {
          address = kv.Value;
          return true;
        }
      }

      return false;
    }

    internal static string NormalizePointKey(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

      return new string(value
        .Trim()
        .Select(NormalizePointKeyChar)
        .ToArray());
    }

    private static char NormalizePointKeyChar(char ch) => ch switch
    {
      'А' => 'A',
      'В' => 'B',
      'С' => 'C',
      'Е' => 'E',
      'К' => 'K',
      'М' => 'M',
      'О' => 'O',
      'Р' => 'P',
      'Т' => 'T',
      'У' => 'Y',
      'Х' => 'X',
      'а' => 'a',
      'е' => 'e',
      'к' => 'k',
      'м' => 'm',
      'о' => 'o',
      'р' => 'p',
      'с' => 'c',
      'т' => 't',
      'у' => 'y',
      'х' => 'x',
      _ => ch
    };
  }

  public class RmPartModel
  {
    public int? PartNumber { get; set; }

    public string SourceText { get; set; } = string.Empty;

    public List<RmPairModel> Pairs { get; set; } = new();
  }
}
