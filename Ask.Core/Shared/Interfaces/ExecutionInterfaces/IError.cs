using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  public interface IError
  {
    IPointError PointErrors { get; }
  }
}
