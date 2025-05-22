using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
  public class EnumDisplayItem
  {
    public string Description { get; set; } = string.Empty;
    public Enum Value { get; set; } = null!;
  }
}
