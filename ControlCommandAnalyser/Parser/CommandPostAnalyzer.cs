using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model;
using Utilities.Models;

namespace ControlCommandAnalyser.Parser
{
  internal class CommandPostAnalyzer
  {
    /// <summary>
    /// Выполняет проверку всех команд и при необходимости добавляет ошибки в модели.
    /// </summary>
    /// <param name="models">Список разобранных команд.</param>
    public static void Analyze(List<BaseCommandModel> models)
    {
      if (models.Count == 0)
        return;

      CheckStartAndEnd(models);
      CheckUniqueMnemonics(models);

      // Извлечение PointsMap из RmCommandModel
      var rmModel = models.OfType<RmCommandModel>().FirstOrDefault();
      Dictionary<string, string> pointsMap = null;
      if (rmModel != null)
      {
        pointsMap = rmModel.PointsMap;
      }

      try
      {
        if (rmModel != null)
        {
          CheckPointLinks(models, pointsMap);
        }
      }
      catch
      { }

      CheckPointExistence(models, pointsMap);
    }

    private static void CheckPointLinks(List<BaseCommandModel> models, Dictionary<string, string> pointsMap)
    {
      var duplicateDestinations = pointsMap
        .GroupBy(kv => kv.Value)
        .Where(g => g.Count() > 1)
        .Select(g => g.Key);

      foreach (var dest in duplicateDestinations)
      {
        var rmModel = models.OfType<RmCommandModel>().FirstOrDefault();
        if (rmModel != null)
        {
          rmModel.Errors.Add(GeneralErrors.DuplicateDestinationPoint(dest, rmModel.StartLineNumber, $"{rmModel.CommandNumber} {rmModel.Mnemonic}"));
        }
      }
    }

    private static void CheckPointExistence(List<BaseCommandModel> models, Dictionary<string, string> pointsMap)
    {
      var pointModels = models.OfType<IHasPoints>();

      foreach (var model in pointModels)
      {
        bool errorPoints = false;

        var baseModel = model as BaseCommandModel;
        for (int i = 0; i < model.Points.Count; i++)
        {
          var point = model.Points[i];
          if (pointsMap == null)
          {
            if (!errorPoints)
            {
              errorPoints = true;
              baseModel?.Errors.Add(GeneralErrors.MissingPointsMap(baseModel.StartLineNumber, $"{baseModel.CommandNumber} {baseModel.Mnemonic}"));
            }

            model.Points[i] = $"Нераспознанная точка: {point}!";
            continue; // пропускаем остальную логику
          }

          if (!pointsMap.ContainsKey(point) && !pointsMap.ContainsValue(point))
          {
            baseModel?.Errors.Add(GeneralErrors.UnknownPoint(point, baseModel.StartLineNumber, $"{baseModel.CommandNumber} {baseModel.Mnemonic}"));
            model.Points[i] = $"Нераспознанная точка: {point}!";
          }
          else
          {
            pointsMap.TryGetValue(point, out var mappedPoint);
            model.Points[i] = mappedPoint ?? point;
          }
        }
      }
    }


    /// <summary>
    /// Проверяет, что первая команда — ОК, а последняя — КЦ. В противном случае добавляет ошибки.
    /// </summary>
    /// <param name="models">Список разобранных команд.</param>
    private static void CheckStartAndEnd(List<BaseCommandModel> models)
    {
      var first = models[0];
      var last = models[^1];

      if (!string.Equals(first.Mnemonic, "ОК", System.StringComparison.OrdinalIgnoreCase))
      {
        first.Errors.Add(GeneralErrors.FirstCommandMustBeOk(first.StartLineNumber, $"{first.CommandNumber} {first.Mnemonic}"));
      }

      if (!string.Equals(last.Mnemonic, "КЦ", System.StringComparison.OrdinalIgnoreCase))
      {
        last.Errors.Add(GeneralErrors.LastCommandMustBeKc(last.StartLineNumber, $"{last.CommandNumber} {last.Mnemonic}"));
      }
    }

    /// <summary>
    /// Проверяет, что указанные команды присутствуют строго по одному разу.
    /// </summary>
    private static void CheckUniqueMnemonics(List<BaseCommandModel> models)
    {
      // Мнемоники, которые должны быть строго один раз
      string[] uniqueMnemonics = { "ОК", "РМ", "СП", "КЦ" };

      foreach (var mnemonic in uniqueMnemonics)
      {
        var matches = models
          .Where(m => string.Equals(m.Mnemonic, mnemonic, StringComparison.OrdinalIgnoreCase))
          .ToList();

        if (matches.Count == 0)
        {
          var first = models[0];
          if (mnemonic != "СП")
          {
            first.Errors.Add(GeneralErrors.MissingRequiredCommand(mnemonic, first.StartLineNumber, $"{first.CommandNumber} {first.Mnemonic}"));
          }
        }
        else if (matches.Count > 1)
        {
          foreach (var duplicate in matches.Skip(1))
          {
            duplicate.Errors.Add(GeneralErrors.DuplicateCommand(mnemonic, duplicate.StartLineNumber, $"{duplicate.CommandNumber} {duplicate.Mnemonic}"));
          }
        }
      }
    }
  }
}
