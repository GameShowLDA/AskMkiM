using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Parsing;

namespace ControlCommandAnalyser.Domain
{
  public class CommandParseResult
  {
    public int LineIndex { get; set; }
    public string CommandNumber { get; set; } = "";
    public string Mnemonic { get; set; } = "";
    public bool IsRecognized { get; set; }

    /// <summary>
    /// Дополнительная подсветка, например "100В"
    /// </summary>
    public List<HighlightRange> ExtraHighlights { get; set; } = new();
  }
}
