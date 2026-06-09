using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  public interface IHasKey
  {
    /// <summary>
    /// Ключи алгоритма проверки, указанные в команде.
    /// </summary>
    public List<string> AlgorithmKey { get; set; }
  }
}
