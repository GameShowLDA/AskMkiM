using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Models;

namespace ControlCommandAnalyser.Model.Chains
{
  /// <summary>
  /// Модель данных, содержащая все цепи схемы.
  /// </summary>
  public class SchemeModel
  {
    public List<ChainModel> ChainModels;

    public SchemeModel(List<ChainModel> chainModel)
    {
      this.ChainModels  = chainModel;
    }
  }
}
