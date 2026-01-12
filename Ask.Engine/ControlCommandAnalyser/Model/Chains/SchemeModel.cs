using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;

namespace Ask.Engine.ControlCommandAnalyser.Model.Chains
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
            if (point.PointType != PointType.Comma)
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

    /// <summary>
    /// Возвращает список списков точек в строковом формате ("x.x.x").
    /// </summary>
    /// <returns>Список списков строковых представлений точек.</returns>
    public List<List<string>> GetPointsDisconnectedAsStrings()
    {
      return ChainDisconnectedPointsMap
        .Values
        .Select(pointList => pointList
          .Select(point => point.ToString())
          .ToList())
        .ToList();
    }

    public List<List<List<PointModel>>> GetPointsConnected()
    {
      var list = ChainConnectedPointsMap.Values.ToList();
      var result = new List<List<List<PointModel>>>();
      foreach (var item in list)
      {
        var list1 = new List<List<PointModel>>();
        foreach (var i in item)
        {
          var list2 = new List<PointModel>();
          foreach (var j in i)
          {
            list2.Add(j);
          }

          list1.Add(list2);
        }
        result.Add(list1);
      }

      return result;
    }

    public List<PointModel> GetAllPointsDisconnected()
    {
      var listPoints = ChainDisconnectedPointsMap.Values.ToList();
      var result = new List<PointModel>();

      foreach (var points in listPoints)
      {
        foreach (var point in points)
        {
          result.Add(point);
        }
      }

      return result;
    }
    #endregion
  }
}
