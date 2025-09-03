using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Model
{
  /// <summary>
  /// Модель для команды РМ: хранит сопоставление точек.
  /// </summary>
  public class RmCommandModel : BaseCommandModel
  {
    public override string Mnemonic => "РМ";

    /// <summary>
    /// Словарь: "точка источника" → "точка назначения".
    /// </summary>
    public Dictionary<string, string> PointsMap { get; set; } = new();

    public IEnumerable<string> ToExpandedLines()
    {
      foreach (var kv in PointsMap)
        yield return $"{kv.Key} => {kv.Value}";
    }

    public List<string> GetAllDestinationPoints()
    {
      return PointsMap.Values.ToList();
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
  }
}
