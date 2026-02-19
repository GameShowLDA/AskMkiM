using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Менеджер разбора схемы точек для команды НЭ.
  /// Извлекает блок точек, формирует модель схемы и применяет знаки включения.
  /// </summary>
  public static class NeSchemeManager
  {
    /// <summary>
    /// Парсит схему точек из тела команды и возвращает модель схемы.
    /// </summary>
    /// <param name="model">Модель команды НЕ.</param>
    /// <param name="rmCommandModel">Модель команды РМ с описанием точек.</param>
    /// <param name="numberLine">Номер строки.</param>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="remainder">Оставшаяся часть команды (очищается от блока точек).</param>
    /// <param name="processedLines">Строки команды без предварительной обработки.</param>
    /// <returns>
    /// Модель схемы, если точки успешно распознаны; иначе <c>null</c>.
    /// </returns>
    public static SchemeModel? Parse(
        NeCommandModel model,
        RmCommandModel rmCommandModel,
        int numberLine,
        string commandNumber,
        string mnemonic,
        ref string remainder,
        IEnumerable<string> processedLines)
    {
      string bodyNoWs = string.Concat(
          processedLines.Select(l => Regex.Replace(l ?? string.Empty, @"\s+", "")));

      int firstStar = bodyNoWs.IndexOf('*');
      int lastStar = bodyNoWs.LastIndexOf('*');

      if (firstStar < 0 || lastStar <= firstStar)
      {
        LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {commandNumber} {mnemonic}");
        model.Errors.Add(NeErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
        return null;
      }

      string pointsBlob = bodyNoWs.Substring(firstStar, lastStar - firstStar + 1);
      model.PointsSourse = pointsBlob;

      LogDebug($"Парсинг точек из общего блока: '{pointsBlob}'");

      var parts = PointParser.ExtractSigns(pointsBlob);

      var finalGroups = new List<GroupModel>();
      var allErrors = new List<ErrorItem>();

      foreach (var part in parts)
      {
        var (parsedScheme, pointErrors) =
            PointParser.ParsePoints(part.CleanExpr, model, rmCommandModel);

        if (pointErrors?.Count > 0)
          allErrors.AddRange(pointErrors);

        if (parsedScheme?.GroupModels == null)
          continue;

        foreach (var group in parsedScheme.GroupModels)
        {
          foreach (var chain in group.ChainModels)
          {
            ApplySign(chain, part.Sign, model);
            finalGroups.Add(new GroupModel(new List<ChainModel> { chain }));
          }
        }
      }

      if (allErrors.Count > 0)
      {
        foreach (var error in allErrors)
        {
          error.SourceLineNumber = numberLine;
          error.Command = $"{commandNumber} {mnemonic}";
          model.Errors.Add(error);

          LogError($"Ошибка точек {commandNumber} {mnemonic}: {error.Description}");
        }
      }

      var scheme = new SchemeModel(finalGroups);

      if (scheme.IsEmpty())
      {
        LogWarning($"Не найдено ни одной точки (строка {numberLine})");
        model.Errors.Add(NeErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
        return null;
      }

      ClearPointsFromRemainder(ref remainder);

      LogInformation(
          $"Схема распознана: цепей={scheme.GroupModels?.Count ?? 0}, частей={scheme.CountParts()}, точек={scheme.CountPoints()}");

      return scheme;
    }

    /// <summary>
    /// Применяет знак включения к цепи.
    /// </summary>
    private static void ApplySign(ChainModel chain, char? sign, NeCommandModel model)
    {
      if (sign == '+')
        model.ElementEnablingType.Add((chain, ElementEnabling.Type.Direct));
      else if (sign == '-')
        model.ElementEnablingType.Add((chain, ElementEnabling.Type.Reverse));
    }

    /// <summary>
    /// Удаляет блок точек из остатка строки команды.
    /// </summary>
    private static void ClearPointsFromRemainder(ref string remainder)
    {
      int first = remainder.IndexOf('*');
      int last = remainder.LastIndexOf('*');

      if (first >= 0 && last > first)
        remainder = remainder[..first].Trim() + remainder[(last + 1)..].Trim();
      else
        remainder = remainder.Trim();
    }
  }
}
