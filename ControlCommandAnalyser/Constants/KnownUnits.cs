using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Constants
{
  public class KnownUnits
  {
    /// <summary>
    /// Допустимые обозначения напряжения, используемые в командах (регистр игнорируется).
    /// </summary>
    public static readonly string[] VoltageUnits = new[]
    {
      "В",   // русская Вольт
    };
  }
}
