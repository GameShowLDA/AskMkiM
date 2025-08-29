using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using System.Text.RegularExpressions;
using Utilities.Models;

namespace ControlCommandAnalyser.Parser
{
  public class PointParser
  {
    public static (SchemeModel, List<ErrorItem>) ParsePoints(string expr, string mnemonic, RmCommandModel rmCommandModel)
    {
      var errors = new List<ErrorItem>();
      var points = new List<PointModel>();
      var partChainModels = new List<PartChainModel>();
      var chainModels = new List<ChainModel>();
      var schemeModel = new SchemeModel(chainModels);
      var status = false;
      // Удаляем пробелы и ведущие/замыкающие *
      expr = expr.Replace(" ", "").Trim('*');
      if (string.IsNullOrEmpty(expr))
      {
        return (null, errors);
      }
      var chainsArray = expr.Split(new[] { "**" }, StringSplitOptions.RemoveEmptyEntries);
      // каждый элемент в chainsArray - это отдельная цепь
      var partChainList = new List<List<string>>();
      // проходимся по цепям и разбиваем их до строк с PartChainModel
      for (int i = 0; i < chainsArray.Length; i++)
      {
        partChainList.Add(CheckIfConnected(chainsArray[i])); // тут получим массив с чаcтями (partChainModel)
      }
      for (int i = 0; i < partChainList.Count; i++)// проходимся по всем цепям
      {
        var pointsRangesList = new List<string>();
        for (int j = 0; j < partChainList[i].Count; j++)//проходимся по массивам с частями цепей
        {
          var pointsStringsList = new List<string>();
          if (!string.IsNullOrEmpty(partChainList[i][j]))
          {
            // получаем точки
            if (partChainList[i][j].Contains(','))
            {
              pointsStringsList = partChainList[i][j].Split(",").ToList();
            }
            else
            {
              pointsStringsList.Add(partChainList[i][j]);
            }
            for (int k = 0; k < pointsStringsList.Count; k++)
            {
              if (pointsStringsList[j].Contains("-"))
              {
                var processedPoints = ProcessedPoints(pointsStringsList[j], errors, mnemonic);
                pointsRangesList.AddRange(processedPoints);
              }
              else
              {
                pointsRangesList.Add(pointsStringsList[j]);
              }
            }
            // теперь точки одной цепи нужно отправить в CommandPostAnalyzer.GetPointsModel чтобы получить соответсвующие точки из РМ
            var result = CommandPostAnalyzer.GetPointsModel(pointsRangesList, rmCommandModel.PointsMap);
            if (!result.Item1)
            {
              points.AddRange(result.Item2);
            }
          }
          partChainModels.Add(new PartChainModel(points));
          points.Clear();
          // в List<PartChainModel> добавить новый элемент с полученными точками(List<PointModel>)
        }
        // добавить в List<ChainModel> полученную цепь (List<PartChainModel>) и перейти к обработке следующей
        chainModels.Add(new ChainModel(partChainModels));
        partChainModels.Clear();
      }
      // готовый List<ChainModel> присвоить SchemeModel
      schemeModel.ChainModels = chainModels;
      return (schemeModel, errors); // а на выходе 
    }

    /// <summary>
    /// Делит строку с точками, в которой есть либо разделители-запятые, либо диапазоны точек.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="errors"></param>
    /// <param name="mnemonic"></param>
    /// <returns>Список моделей точек.</returns>
    private static List<string> ProcessedPoints(string token, List<ErrorItem> errors, string mnemonic)
    {
      var result = new List<string>();
      var startToken = token.Substring(0, token.IndexOf("-") + 1);
      var endToken = token.Substring(token.IndexOf("-") + 1);
      var rangeMatch = Regex.Match(startToken, @"^(?<prefix>.+?/)(?<start>\d+)-$");
      if (!rangeMatch.Success)
      {
        rangeMatch = Regex.Match(startToken, @"(?<start>\d+)-$");
        if (rangeMatch.Success)
        {
          endToken = token.Substring(token.IndexOf("-") + 1);
        }
      }
      if (rangeMatch.Success && !string.IsNullOrEmpty(endToken))
      {
        if (Regex.IsMatch(endToken, @"^\d+$"))
        {
          string prefix = rangeMatch.Groups["prefix"].Value;
          int start = int.Parse(rangeMatch.Groups["start"].Value);
          int end = int.Parse(endToken);

          // Добавляем все точки диапазона
          for (int n = start; n <= end; n++)
          {
            if (!string.IsNullOrEmpty(prefix))
            {
              result.Add($"{prefix}{n}");
            }
            else
            {
              result.Add($"{n}");
            }
          }
        }
      }
      else if ((token.Length == 1 || token.Length > 1 && !token.Any(c => new[] { '\\', '-', '/', '.', ',' }.Contains(c))) && mnemonic == "КС")
      {
        errors.Add(new ErrorItem
        {
          Description = $"Нельзя указывать одиночную точку (точка: {token}).",
          Code = Utilities.Errors.ErrorCode.Gen_InvalidOnePointUse,
        });
      }
      return result;
    }

    /// <summary>
    /// находит сообщенные точки, если таковые есть.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private static List<string> CheckIfConnected(string token)
    {
      if (token.Contains('#'))
      {
        var tokens = token.Split('#');
        return tokens.ToList();
      }
      else
      {
        var newList = new List<string> { token };
        return newList;
      }
    }
  }
}
