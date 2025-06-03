using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parsing.Model
{
  /// <summary>
  /// Модель для команды СИ: напряжение, сопротивление, время.
  /// </summary>
  public class SiCommandModel : BaseCommandModel
  {
    public int Voltage { get; set; } = -1;
    public int Resistance { get; set; } = -1;
    public int Time { get; set; } = -1;
  }
}
