using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Interfaces.ParserInterfaces
{
  public interface IHasAmperage
  {
    double? Amperage { get; set; }

    string? AmperageUnit { get; set; }
    string? AmperageSource { get; set; }
    bool HasAmperage { get; set; }
  }
}
