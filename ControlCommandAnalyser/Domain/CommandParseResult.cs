using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Domain
{
  public class CommandParseResult
  {
    public int LineIndex { get; set; }           // Номер строки
    public string Mnemonic { get; set; } = "";
    public string CommandNumber { get; set; } = "";
    public bool IsRecognized { get; set; }       // Найдена ли команда
  }
}
