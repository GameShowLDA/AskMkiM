using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Models
{
  public class ErrorItem
  {
    public int LineNumber { get; set; }
    public string Command { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
  }
}
