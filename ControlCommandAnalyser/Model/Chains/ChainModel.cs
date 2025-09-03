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

    public override string ToString()
    {
      var str = string.Empty;
      foreach (var pointModel in PointModels)
      {
        switch (pointModel.PointType)
        {
          case PointType.Type.Star:
            str += $"*{pointModel.Mnemonic}";
            break;
          case PointType.Type.Comma:
            str += $",{pointModel.Mnemonic}";
            break;
          case PointType.Type.Hash:
            str += $"#{pointModel.Mnemonic}";
            break;
        }
      }

      str += "*";
      return str;
    }
  }
}
