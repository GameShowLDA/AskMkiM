using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.DTO.Devices.RelaySwitchModule
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
    private Dictionary<GroupModel, GroupModel> ChainConnectedPointsMap = new();

    /// <summary>
    /// Словарь цепей и списков разобщенных точек.
    /// </summary>
    private Dictionary<GroupModel, ChainModel> ChainDisconnectedPointsMap = new();

    /// <summary>
    /// Словарь цепей и списков разобщенных точек.
    /// </summary>
    private List<(ChainModel, ChainModel)> ErrorChainDisconnectedPointsMap { get; set; }

    private List<(ChainModel A, ChainModel B)> _normalizedErrors = new();

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
          SetPointsConnected(chain, disconnectedPoint);
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
          SetPointsDisconnected(group, disconnectPoint);
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
    public void SetPointsConnected(GroupModel groupModel, GroupModel groupModel1)
    {
      ChainConnectedPointsMap.Add(groupModel, groupModel1);
    }

    public void SetPointsDisconnected(GroupModel groupModel, ChainModel chainModel)
    {
      ChainDisconnectedPointsMap.Add(groupModel, chainModel);
    }

    public void SetAllPoints(PointModel pointModel)
    {
      AllPoint.PointModels.Add(pointModel);
    }

    public GroupModel GetPointsDisconnected()
    {
      var allChains = ChainDisconnectedPointsMap.Values.ToList();

      if (_normalizedErrors == null || _normalizedErrors.Count == 0)
        return new GroupModel(allChains);

      var graph = BuildGraph(allChains, _normalizedErrors);
      var components = FindConnectedComponents(allChains, graph);

      return new GroupModel(components
          .Select(c => MergeChainModels(c))
          .ToList());
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

    public void SetErrorChainDisconnectedPoints(List<(ChainModel, ChainModel)> errors)
    {
      _normalizedErrors = errors
          ?.Select(e => (A: e.Item1, B: e.Item2))
          .ToList()
          ?? new List<(ChainModel, ChainModel)>();
    }

    public void SetErrorChainDisconnectedPoints(List<ChainModel> errors)
    {
      _normalizedErrors = new List<(ChainModel, ChainModel)>();

      if (errors == null || errors.Count < 2)
        return;

      for (int i = 0; i < errors.Count - 1; i++)
      {
        _normalizedErrors.Add((errors[i], errors[i + 1]));
      }
    }

    /// <summary>
    /// Возвращает нормализованный список разорванных цепей 
    /// в виде списка пар (ChainModel A, ChainModel B).
    /// </summary>
    /// <returns>Список нормализованных пар разорванных точек.</returns>
    public List<(ChainModel A, ChainModel B)> GetErrorChainDisconnectedPoints()
    {
      return _normalizedErrors ?? new List<(ChainModel A, ChainModel B)>();
    }

    #endregion

    private ChainModel MergeChainModels(List<ChainModel> chains)
    {
      var merged = new ChainModel();

      foreach (var chain in chains)
        merged.PointModels.AddRange(chain.PointModels);

      return merged;
    }

    private Dictionary<ChainModel, HashSet<ChainModel>> BuildGraph(
    List<ChainModel> points,
    List<(ChainModel A, ChainModel B)> edges)
    {
      var adjacency = new Dictionary<ChainModel, HashSet<ChainModel>>();

      foreach (var p in points)
        adjacency[p] = new HashSet<ChainModel>();

      foreach (var (a, b) in edges)
      {
        adjacency[a].Add(b);
        adjacency[b].Add(a);
      }

      return adjacency;
    }

    private List<List<ChainModel>> FindConnectedComponents(
    List<ChainModel> points,
    Dictionary<ChainModel, HashSet<ChainModel>> graph)
    {
      var result = new List<List<ChainModel>>();
      var visited = new HashSet<ChainModel>();

      foreach (var start in points)
      {
        if (visited.Contains(start))
          continue;

        var stack = new Stack<ChainModel>();
        var component = new List<ChainModel>();

        stack.Push(start);
        visited.Add(start);

        while (stack.Count > 0)
        {
          var node = stack.Pop();
          component.Add(node);

          foreach (var neighbor in graph[node])
          {
            if (!visited.Contains(neighbor))
            {
              visited.Add(neighbor);
              stack.Push(neighbor);
            }
          }
        }

        result.Add(component);
      }

      return result;
    }
  }
}
