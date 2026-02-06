using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser
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
      CheckUpLabels(models);

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

    public static (bool, List<PointModel>) GetPointsModel(List<string> points, Dictionary<string, string> pointsMap)
    {
      List<PointModel> pointModels = new List<PointModel>();
      bool error = false;

      foreach (var point in points)
      {
        string pointPart = pointsMap.GetValueOrDefault(point, string.Empty);
        if (string.IsNullOrEmpty(pointPart))
        {
          error = true;
          continue;
        }

        var pointModel = PointModel.ParsePointString(pointPart);
        pointModels.Add(pointModel);
      }

      return (error, pointModels);
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

    private static void CheckUpLabels(List<BaseCommandModel> models)
    {
      var allNumbers = new HashSet<string>(models.Select(m => m.CommandNumber));

      foreach (var up in models.OfType<UpCommandModel>())
      {
        if (string.IsNullOrWhiteSpace(up.TargetLabel))
        {
          up.Errors.Add(UpErrors.MissingOrInvalidLabel(up.StartLineNumber, $"{up.CommandNumber} {up.Mnemonic}"));
          continue;
        }

        if (!allNumbers.Contains(up.TargetLabel))
        {
          up.Errors.Add(UpErrors.LabelNotFound(up.TargetLabel, up.StartLineNumber, $"{up.CommandNumber} {up.Mnemonic}"));
          continue;
        }

        if (!int.TryParse(up.TargetLabel, out int targetNumber))
        {
          up.Errors.Add(UpErrors.LabelIsNotNumber(up.TargetLabel, up.StartLineNumber, $"{up.CommandNumber} {up.Mnemonic}"));
          continue;
        }

        if (!int.TryParse(up.CommandNumber, out int currentNumber))
        {
          continue;
        }

        if (targetNumber <= currentNumber)
        {
          up.Errors.Add(UpErrors.LabelLessOrEqual(up.TargetLabel, up.CommandNumber, up.StartLineNumber, $"{up.CommandNumber} {up.Mnemonic}"));
        }
      }
    }
  }
}
