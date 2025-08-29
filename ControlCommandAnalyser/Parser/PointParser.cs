using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using System.Text.RegularExpressions;
using Utilities.Models;

namespace ControlCommandAnalyser.Parser
{
  public class PointParser
  {

    public static (PartChainModel, List<ErrorItem>) ParsePoints(string expr, string mnemonic, RmCommandModel rmCommandModel)
    {
      var points = new List<PointModel>();
      var errors = new List<ErrorItem>();
      var status = false;
      // Удаляем пробелы и ведущие/замыкающие *
      expr = expr.Replace(" ", "").Trim('*');
      if (string.IsNullOrEmpty(expr))
      {
        return (new PartChainModel(points), errors);
      }
      var chainsArray = expr.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries); // тут получаются chain
      var partChainList = new List<string>();
      for (int i = 0; i < chainsArray.Length; i++)
      {
        partChainList.AddRange(CheckIfConnected(chainsArray[i]));
      }
      var pointsStringsList = new List<string>();
      var pointsRangesList = new List<string>();
      for (int i = 0; i < partChainList.Count; i++)
      {
        if (!string.IsNullOrEmpty(partChainList[i]))
        {
          if (partChainList[i].Contains(','))
          {
            pointsStringsList = partChainList[i].Split(",").ToList();
          }
          else
          {
            pointsStringsList = partChainList[i].Split(",").ToList();
          }
          for (int j = 0; j < pointsStringsList.Count; j++)
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
          var result = CommandPostAnalyzer.GetPointsModel(pointsRangesList, rmCommandModel.PointsMap);
          if (!result.Item1)
          {
            points.AddRange(result.Item2);
          }
        }

      }
      return (new PartChainModel(points), errors); // а на выходе 
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
