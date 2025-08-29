using Utilities.Models;

namespace ControlCommandAnalyser.Model.Chains
{
  /// <summary>
  /// Модель данных, содержащая точки части цепи.
  /// </summary>
  public class PartChainModel
  {
    public List<PointModel> PointModels;

    public PartChainModel(List<PointModel> pointModels) 
    {
      this.PointModels = pointModels;
    }
  }
}
