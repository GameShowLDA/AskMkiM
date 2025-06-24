using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Errors;

namespace Utilities.Models
{
  public class ErrorItem
  {
    public int SourceLineNumber { get; set; }
    public int FormattedLineNumber { get; set; }
    public string Command { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ErrorCode? Code { get; set; }
  }
}
