using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Models;

namespace ControlCommandAnalyser.Model.Chains
{
  /// <summary>
  /// Модель данных, содержащая все части цепей в одной цепи.
  /// </summary>
  public class ChainModel
  {
    public List<PartChainModel> ChainModels;

    public ChainModel(List<PartChainModel> partChainModels)
    {
      this.ChainModels = partChainModels;
    }
  }
}
