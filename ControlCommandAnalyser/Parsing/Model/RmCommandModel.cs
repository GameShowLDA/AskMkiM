using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parsing.Model
{
  internal class RmCommandModel : BaseCommandModel
  {
    /// <summary>
    /// Словарь: ключ — исходная точка (например, "Х17/1"), значение — соответствующая точка (например, "1.1.60").
    /// </summary>
    public Dictionary<string, string> PointsMap { get; set; } = new();
  }
}
