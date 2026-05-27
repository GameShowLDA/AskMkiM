using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.DTO.Devices.RelaySwitchModule
{
  /// <summary>
  /// Модель данных, содержащая все части цепей в одной цепи.
  /// </summary>
  public class GroupModel
  {
    /// <summary>
    /// Пользовательское имя цепи, указанное в ССИРТ перед символом "=".
    /// </summary>
    public string? ChainName { get; set; }

    public List<ChainModel> ChainModels;

    public GroupModel(List<ChainModel> chainModels)
    {
      this.ChainModels = chainModels;
    }
    public GroupModel()
    {
      ChainModels = new List<ChainModel>();
    }

  }
}
