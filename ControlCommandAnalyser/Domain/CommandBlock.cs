using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Domain
{
  public class CommandBlock
  {
    public int StartLine { get; set; }
    public List<string> Lines { get; set; } = new();
  }
}
