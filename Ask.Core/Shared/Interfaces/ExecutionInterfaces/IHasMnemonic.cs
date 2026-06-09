using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  public interface IHasMnemonic
  {
    /// <summary>
    /// Номер команды.
    /// </summary>
    string CommandNumber { get; set; }

    /// <summary>
    /// Мнемоника команды.
    /// </summary>
    string Mnemonic { get; set; }
  }
}
