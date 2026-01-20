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
    public Dictionary<GroupModel, GroupModel> ChainConnectedPointsMap = new();

    /// <summary>
    /// Словарь цепей и списков разобщенных точек.
    /// </summary>
    private Dictionary<GroupModel, ChainModel> ChainDisconnectedPointsMap = new();

    /// <summary>
    /// Список всех точек в схеме.
    /// </summary>
    private ChainModel AllPoint;

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
      AllPoint = new ChainModel();
      foreach (var pair in GroupModels)
      {
        foreach (var part in pair.ChainModels)
        {
          AllPoint.PointModels.AddRange(part.PointModels);
        }
      }
    }

    private void InitializeCommunnicatedPoints()
    {
      foreach (var chain in GroupModels)
      {
        var disconnectedPoint = new GroupModel();
        foreach (var parts in chain.ChainModels)
        {
          if (parts.PointModels.Count > 1)
          {
            var list = new ChainModel();
            list.PointModels.AddRange(parts.PointModels);
            disconnectedPoint.ChainModels.Add(list);
          }
        }

        if (disconnectedPoint.ChainModels.Count > 0)
        {
          ChainConnectedPointsMap.Add(chain, disconnectedPoint);
        }

      }
    }

    private void InitializeDisconnectedPoint()
    {
      foreach (var group in GroupModels)
      {
        var disconnectPoint = new ChainModel();
        foreach (var chain in group.ChainModels)
        {
          foreach (var point in chain.PointModels)
          {
            if (point.PointType != PointType.Comma)
            {
              disconnectPoint.PointModels.Add(point);
            }
          }
        }

        if (disconnectPoint.PointModels.Count > 0)
        {
          ChainDisconnectedPointsMap.Add(group, disconnectPoint);
        }
      }
    }
    public GroupModel GetPointsConnected(GroupModel groupModel)
    {
      return ChainConnectedPointsMap.GetValueOrDefault(groupModel);
    }

    public ChainModel GetPointsDisconnected(GroupModel groupModel)
    {
      return ChainDisconnectedPointsMap.GetValueOrDefault(groupModel);
    }

    public GroupModel GetPointsDisconnected()
    {
      return new GroupModel(ChainDisconnectedPointsMap.Values.ToList());
    }

    /// <summary>
    /// Возвращает список списков точек в строковом формате ("x.x.x").
    /// </summary>
    /// <returns>Список списков строковых представлений точек.</returns>
    public List<List<string>> GetPointsDisconnectedAsStrings()
    {
      return ChainDisconnectedPointsMap
        .Values
        .Select(pointList => pointList.PointModels
          .Select(point => point.ToString())
          .ToList())
        .ToList();
    }

    public List<GroupModel> GetPointsConnected()
    {
      var list = ChainConnectedPointsMap.Values.ToList();
      var result = new List<GroupModel>();
      foreach (var item in list)
      {
        var list1 = new GroupModel();
        foreach (var i in item.ChainModels)
        {
          var list2 = new ChainModel();
          foreach (var j in i.PointModels)
          {
            list2.PointModels.Add(j);
          }

          list1.ChainModels.Add(list2);
        }
        result.Add(list1);
      }

      return result;
    }

    public ChainModel GetAllPointsDisconnected()
    {
      var listPoints = ChainDisconnectedPointsMap.Values.ToList();
      var result = new ChainModel();

      foreach (var points in listPoints)
      {
        foreach (var point in points.PointModels)
        {
          result.PointModels.Add(point);
        }
      }

      return result;
    }
    #endregion
  }
}
