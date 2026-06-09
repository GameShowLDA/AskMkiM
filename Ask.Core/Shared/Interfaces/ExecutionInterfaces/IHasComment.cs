using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  public interface IHasComment
  {
    /// <summary>
    /// Комментарии, указанные в команде.
    /// </summary>
    public List<string> Comment { get; set; }
  }
}
