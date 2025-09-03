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
    /// Словарь цепей и списков сообщенных точек.
    /// </summary>
    public Dictionary<GroupModel, List<List<PointModel>>> ChainConnectedPointsMap = new();

    /// <summary>
    /// Словарь цепей и списков разобщенных точек.
    /// </summary>
    private Dictionary<GroupModel, List<PointModel>> ChainDisconnectedPointsMap = new();

    /// <summary>
    /// Список всех точек в схеме.
    /// </summary>
    private List<PointModel> AllPoint;

    public SchemeModel(List<GroupModel> chainModel)
    {
      this.GroupModels = chainModel;
      InitializePoints();
    }

    #region Инициализация точек.
    private void InitializePoints()
    {
      InitializeAllPoint();
      InitializeCommunnicatedPoints();
      InitializeDisconnectedPoint();
    }

    /// <summary>
    /// Инициализация всех точек схемы.
    /// </summary>
    private void InitializeAllPoint()
    {
      AllPoint = new List<PointModel>();
      foreach (var pair in GroupModels)
      {
        foreach (var part in pair.ChainModels)
        {
          AllPoint.AddRange(part.PointModels);
        }
      }
    }

    private void InitializeCommunnicatedPoints()
    {
      foreach (var chain in GroupModels)
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
          ChainConnectedPointsMap.Add(chain, disconnectedPoint);
        }

      }
    }

    private void InitializeDisconnectedPoint()
    {
      foreach (var group in GroupModels)
      {
        List<PointModel> disconnectPoint = new List<PointModel>();
        foreach (var chain in group.ChainModels)
        {
          foreach (var point in chain.PointModels)
          {
            if (point.PointType != PointType.Type.Comma)
            {
              disconnectPoint.Add(point);
            }
          }
        }

        if (disconnectPoint.Count > 0)
        {
          ChainDisconnectedPointsMap.Add(group, disconnectPoint);
        }
      }
    }

    public List<List<PointModel>> GetPointsConnected(GroupModel groupModel)
    {
      return ChainConnectedPointsMap.GetValueOrDefault(groupModel);
    }

    public List<PointModel> GetPointsDisconnected(GroupModel groupModel)
    {
      return ChainDisconnectedPointsMap.GetValueOrDefault(groupModel);
    }

    public List<List<PointModel>> GetPointsDisconnected()
    {
      return ChainDisconnectedPointsMap.Values.ToList();
    }
    #endregion
  }
}
