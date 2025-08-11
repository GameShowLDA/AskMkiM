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
  }
}
