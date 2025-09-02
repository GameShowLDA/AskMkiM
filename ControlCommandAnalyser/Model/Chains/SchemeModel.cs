using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Theme;
using Utilities.Models;

namespace ControlCommandAnalyser.Model.Chains
{
  /// <summary>
  /// Модель данных, содержащая все цепи схемы.
  /// </summary>
  public class SchemeModel
  {
    public List<ChainModel> ChainModels;

    private List<List<PointModel>> PairPoint;

    private Dictionary<ChainModel, List<PointModel>> keyValuePairs;

    private List<PointModel> AllPoint;

    public SchemeModel(List<ChainModel> chainModel)
    {
      this.ChainModels = chainModel;
      PairPoint = new();
      InitializePairPoint(chainModel);
    }

    private void InitializePairPoint(List<ChainModel> chainModel)
    {
      keyValuePairs = new();
      foreach (var chain in chainModel)
      {
        if (chain.ChainModels.Count <= 1)
        {
          continue;
        }

        PairPoint.Add(new List<PointModel>());
        var pairPoint = PairPoint.Last();

        for (int i = 0; i < chain.ChainModels.Count; i++)
        {
          if (i + 1 >= chain.ChainModels.Count)
          {
            break;
          }

          AddPairPoint(pairPoint, chain.ChainModels[i].PointModels.LastOrDefault(), chain.ChainModels[i + 1].PointModels.FirstOrDefault());
        }
        keyValuePairs.Add(chain, pairPoint);
      }

      AllPoint = new List<PointModel>();
      foreach (var pair in ChainModels)
      {
        foreach (var part in pair.ChainModels)
        {
          AllPoint.AddRange(part.PointModels);
        }
      }
    }
    private void AddPairPoint(List<PointModel> pairPoint, PointModel? a, PointModel? b)
    {
      if (a == null || b == null)
      {
        return;
      }

      if (!pairPoint.Contains(a))
      {
        pairPoint.Add(a);
      }

      if (!pairPoint.Contains(b))
      {
        pairPoint.Add(b);
      }
    }

    public bool TryPairPointAllChain(PointModel pointModel, out List<PointModel>? result)
    {
      result = new List<PointModel>();

      foreach (var item in ChainModels)
      {
        if (TryPairPoint(item, pointModel, out List<PointModel> result2))
        {
          result = result2;
          return true;
        }
      }

      return false;
    }

    public bool TryPairPoint(ChainModel chain, PointModel pointModel, out List<PointModel>? result)
    {
      result = keyValuePairs.GetValueOrDefault(chain, null);

      if (result == null || !result.Contains(pointModel))
      {
        result = null;
        return false;
      }

      return true;
    }

    public List<PointModel> GetAllPoints()
    {
      return AllPoint.ToList();
    }
  }
}
