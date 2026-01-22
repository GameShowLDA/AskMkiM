using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;

namespace Ask.Engine.ControlCommandAnalyser.Model.Chains
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

    public ChainModel()
    {
      PointModels = new List<PointModel>();
    }

    public override string ToString()
    {
      var str = string.Empty;
      foreach (var pointModel in PointModels)
      {
        switch (pointModel.PointType)
        {
          case PointType.Star:
            str += $"*{pointModel.Mnemonic}";
            break;
          case PointType.Comma:
            str += $",{pointModel.Mnemonic}";
            break;
          case PointType.Hash:
            str += $"#{pointModel.Mnemonic}";
            break;
        }
      }

      str += "*";
      return str;
    }
  }
}
