using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Helpers
{
  public static class SchemeManager
  {
    public static SchemeModel GetScheme(BaseCommandModel model, RmCommandModel rmCommandModel, int numberLine, ref string remainder)
    {
      string bodyNoWs = Regex.Replace(remainder ?? string.Empty, @"\s+", "");

      if (!TryExtractPointsBlock(bodyNoWs, out var firstStar, out var lastStar))
      {
        HandleNoPointsBlock(model, numberLine);
        return null;
      }

      var scheme = ParseScheme(model, bodyNoWs, rmCommandModel, firstStar, lastStar, numberLine);

      remainder = ClearLineFromPoints(remainder);

      return scheme;
    }

    private static bool TryExtractPointsBlock( string body, out int firstStar, out int lastStar)
    {
      firstStar = body.IndexOf('*');
      lastStar = body.LastIndexOf('*');

      return firstStar >= 0 && lastStar > firstStar;
    }

    private static void HandleNoPointsBlock(BaseCommandModel model, int numberLine)
    {
      LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {model.CommandNumber} {model.Mnemonic}");

      model.Errors.Add(EhtErrors.EmptyPoints(numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
    }

    private static SchemeModel? ParseScheme(
    BaseCommandModel model,
    string body,
    RmCommandModel rmCommandModel,
    int firstStar,
    int lastStar,
    int numberLine)
    {
      var pointsBlob = body.Substring(firstStar, lastStar - firstStar + 1);
      model.PointsSourse = pointsBlob;

      LogDebug($"Парсинг точек из общего блока: '{pointsBlob}'");

      var (scheme, pointErrors) = PointParser.ParsePoints(pointsBlob, model, rmCommandModel);

      CheckPointsErrors(model, numberLine, pointErrors);

      if (scheme == null || scheme.IsEmpty())
      {
        HandleEmptyScheme(model, numberLine);
        return null;
      }

      LogInformation(
          $"Схема распознана: цепей={scheme.GroupModels?.Count ?? 0}, частей={scheme.CountParts()}, точек={scheme.CountPoints()}");

      return scheme;
    }

    private static void HandleEmptyScheme(BaseCommandModel model, int numberLine)
    {
      LogWarning(
          $"Не найдено ни одной точки (строка {numberLine}): {model.CommandNumber} {model.Mnemonic}");

      model.Errors.Add(
          EhtErrors.EmptyPoints(numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
    }



    private static void CheckPointsErrors(BaseCommandModel model, int numberLine, List<ErrorItem> pointErrors)
    {
      if (pointErrors?.Count > 0)
      {
        foreach (var error in pointErrors)
        {
          error.SourceLineNumber = numberLine;
          error.Command = $"{model.CommandNumber} {model.Mnemonic}";
          model.Errors.Add(error);
          LogError(
             $"При парсинге точек команды {model.CommandNumber} {model.Mnemonic} произошла ошибка: {error.Description} (строка {error.SourceLineNumber}).");
        }
      }
    }

    private static string ClearLineFromPoints(string remainder)
    {
      int idxStarInFirstLine = remainder.IndexOf('*');
      int idxStarInSecondLine = remainder.LastIndexOf('*');
      if (idxStarInFirstLine >= 0 && idxStarInSecondLine > idxStarInFirstLine)
      {
        remainder =
            remainder[..idxStarInFirstLine].Trim()
            + remainder[(idxStarInSecondLine + 1)..].Trim();
      }
      else
      {
        remainder = remainder.Trim();
      }

      return remainder;
    }
  }
}
