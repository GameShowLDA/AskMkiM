using Utilities.Models;

namespace ControlCommandAnalyser.Model.Chains
{
  /// <summary>
  /// Модель данных, содержащая точки части цепи.
  /// </summary>
  public class ChainModel
  {
    public List<PointModel> PointModels;

    public ChainModel(List<PointModel> pointModels) 
    {
      this.PointModels = pointModels;
    }
  }
}
