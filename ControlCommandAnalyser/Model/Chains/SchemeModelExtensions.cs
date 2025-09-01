using System.Collections.Generic;
using System.Linq;
using Utilities.Models;

namespace ControlCommandAnalyser.Model.Chains
{
  /// <summary>
  /// Расширения для работы со схемой: проверка пустоты, подсчёты и обход точек.
  /// </summary>
  public static class SchemeModelExtensions
  {
    /// <summary>
    /// Возвращает true, если в схеме нет ни одной точки.
    /// </summary>
    public static bool IsEmpty(this SchemeModel? scheme)
    {
      if (scheme?.ChainModels == null || scheme.ChainModels.Count == 0)
        return true;

      foreach (var chain in scheme.ChainModels)
      {
        if (chain?.ChainModels == null || chain.ChainModels.Count == 0)
          continue;

        foreach (var part in chain.ChainModels)
        {
          if (part?.PointModels != null && part.PointModels.Count > 0)
            return false;
        }
      }
      return true;
    }

    /// <summary>
    /// Считает количество частей (PartChainModel) во всех цепях схемы.
    /// </summary>
    public static int CountParts(this SchemeModel? scheme)
    {
      if (scheme?.ChainModels == null) return 0;
      int parts = 0;
      foreach (var chain in scheme.ChainModels)
        parts += chain?.ChainModels?.Count ?? 0;
      return parts;
    }

    /// <summary>
    /// Считает количество точек во всех частях всех цепей.
    /// </summary>
    public static int CountPoints(this SchemeModel? scheme)
    {
      if (scheme?.ChainModels == null) return 0;
      int cnt = 0;
      foreach (var chain in scheme.ChainModels)
        foreach (var part in chain?.ChainModels ?? Enumerable.Empty<PartChainModel>())
          cnt += part?.PointModels?.Count ?? 0;
      return cnt;
    }

    /// <summary>
    /// Последовательно перечисляет все точки схемы.
    /// Удобно для логирования/дальнейшей обработки.
    /// </summary>
    public static IEnumerable<PointModel> EnumeratePoints(this SchemeModel? scheme)
    {
      if (scheme?.ChainModels == null) yield break;

      foreach (var chain in scheme.ChainModels)
      {
        if (chain?.ChainModels == null) continue;

        foreach (var part in chain.ChainModels)
        {
          if (part?.PointModels == null) continue;

          foreach (var p in part.PointModels)
            if (p != null) yield return p;
        }
      }
    }
  }
}
