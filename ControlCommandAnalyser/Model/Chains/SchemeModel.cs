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
    /// <summary>
    /// Список цепей.
    /// </summary>
    public List<GroupModel> GroupModels;

    /// <summary>
    /// Словарь цепей и списков связанных точек.
    /// </summary>
    private Dictionary<ChainModel, List<PointModel>> ChainConnectedPointsMap = new();

    /// <summary>
    /// Словарь цепей и списков разобщенных точек.
    /// </summary>
    private Dictionary<ChainModel, List<PointModel>> ChainDisconnectedPointsMap = new();

    /// <summary>
    /// Словарь цепей и списков сообщенных точек.
    /// </summary>
    private Dictionary<ChainModel, List<List<PointModel>>> ChainCommunicatedPointsMap = new();


    /// <summary>
    /// Список всех точек в схеме.
    /// </summary>
    private List<PointModel> AllPoint;

    public SchemeModel(List<ChainModel> chainModel)
    {
      this.GroupModels  = chainModel;
      InitializePoints();
    }

    #region Инициализация точек.
    private void InitializePoints()
    {
      InitializeAllPoint();
      InitializeDisconnectedPoint();
      InitializeConnectedPoints();
      InitializeCommunnicatedPoints();
    }

    /// <summary>
    /// Инициализация всех точек схемы.
    /// </summary>
    private void InitializeAllPoint()
    {
      AllPoint = new List<PointModel>();
      foreach (var pair in ChainModels)
      {
        foreach (var part in pair.ChainModels)
        {
          AllPoint.AddRange(part.PointModels);
        }
      }
    }

    /// <summary>
    /// Инициализация разобщенных точек.
    /// </summary>
    private void InitializeDisconnectedPoint()
    {
      foreach (var chain in ChainModels)
      {
        var disconnectedPoint = new List<PointModel>();
        foreach (var parts in chain.ChainModels)
        {
          disconnectedPoint.Add(parts.PointModels[0]);
        }

        ChainDisconnectedPointsMap.Add(chain, disconnectedPoint);
      }
    }

    /// <summary>
    /// Инициализация сообщенных точек.
    /// </summary>
    private void InitializeConnectedPoints()
    {
      foreach (var chain in ChainModels)
      {
        if (chain.ChainModels.Count <= 1)
        {
          continue;
        }

        var pairPoint = new List<PointModel>();

        for (int i = 0; i < chain.ChainModels.Count; i++)
        {
          if (i + 1 >= chain.ChainModels.Count)
          {
            break;
          }

          AddPairPoint(pairPoint, chain.ChainModels[i].PointModels.LastOrDefault(), chain.ChainModels[i + 1].PointModels.FirstOrDefault());
        }
        ChainConnectedPointsMap.Add(chain, pairPoint);
      }
    }

    private void InitializeCommunnicatedPoints()
    {
      foreach (var chain in ChainModels)
      {
        var disconnectedPoint = new List<List<PointModel>>();
        foreach (var parts in chain.ChainModels)
        {
          if (parts.PointModels.Count > 1)
          {
            var list = new List<PointModel>();
            list.AddRange(parts.PointModels);
            disconnectedPoint.Add(list);
          }
        }

        if (disconnectedPoint.Count > 0)
        {
          ChainCommunicatedPointsMap.Add(chain, disconnectedPoint);
        }

      }
    }
    #endregion

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

    public bool TryCommunicatedPointAllChain(PointModel pointModel, out List<PointModel>? result)
    {
      result = new List<PointModel>();

      foreach (var item in ChainModels)
      {
        if (TryCommunicatedPoint(item, pointModel, out List<PointModel> result2))
        {
          result = result2;
          return true;
        }
      }

      return false;
    }

    private bool TryCommunicatedPoint(ChainModel chain, PointModel pointModel, out List<PointModel>? result)
    {
      result = ChainConnectedPointsMap.GetValueOrDefault(chain, null);

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
