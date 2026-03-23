using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser
{
  /// <summary>
  /// Выполняет пост-анализ разобранных команд.
  /// <para>
  /// Проверяет корректность структуры сценария:
  /// <list type="bullet">
  /// <item><description>наличие обязательных команд;</description></item>
  /// <item><description>уникальность мнемоник;</description></item>
  /// <item><description>корректность переходов по меткам;</description></item>
  /// <item><description>связи точек РМ.</description></item>
  /// </list>
  /// </para>
  /// </summary>
  internal class CommandPostAnalyzer
  {
    /// <summary>
    /// Запускает полный набор пост-проверок для списка команд.
    /// </summary>
    /// <param name="models">Список разобранных моделей команд.</param>
    /// <remarks>
    /// Последовательно выполняет:
    /// <list type="number">
    /// <item><description>проверку первой и последней команды;</description></item>
    /// <item><description>проверку уникальности обязательных мнемоник;</description></item>
    /// <item><description>проверку корректности меток УП;</description></item>
    /// <item><description>проверку связей точек через РМ.</description></item>
    /// </list>
    /// </remarks>
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

    /// <summary>
    /// Проверяет, что у точек РМ нет повторяющихся конечных точек назначения.
    /// </summary>
    /// <param name="models">Список команд.</param>
    /// <param name="pointsMap">Словарь соответствия точек.</param>
    /// <remarks>
    /// При обнаружении повторов добавляет ошибки в модель команды РМ.
    /// </remarks>
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

    /// <summary>
    /// Преобразует список строковых обозначений точек в модели точек.
    /// </summary>
    /// <param name="points">Список обозначений точек.</param>
    /// <param name="pointsMap">Словарь соответствий точек из команды РМ.</param>
    /// <returns>
    /// Кортеж:
    /// <list type="bullet">
    /// <item><description><c>bool</c> — признак наличия ошибок сопоставления;</description></item>
    /// <item><description>список моделей точек.</description></item>
    /// </list>
    /// </returns>
    public static (List<ErrorItem>, List<PointModel>) GetPointsModel(List<string> points, BaseCommandModel model, Dictionary<string, string> pointsMap)
    {
      List<PointModel> pointModels = new List<PointModel>();
      List<ErrorItem> error = new();

      foreach (var point in points)
      {
        string pointPart = pointsMap.GetValueOrDefault(point, string.Empty);
        if (string.IsNullOrEmpty(pointPart))
        {
          error.Add(GeneralErrors.UnknownPoint(point, model.StartLineNumber, $"{model.CommandNumber} {model.Mnemonic}"));
          continue;
        }

        var pointModel = PointModel.ParsePointString(pointPart);
        pointModels.Add(pointModel);
      }

      return (error, pointModels);
    }

    /// <summary>
    /// Проверяет, что первая команда — «ОК», а последняя — «КЦ».
    /// </summary>
    /// <param name="models">Список команд.</param>
    /// <remarks>
    /// Если условие нарушено, ошибки добавляются в соответствующие модели.
    /// </remarks>
    private static void CheckStartAndEnd(List<BaseCommandModel> models)
    {
      var first = models[0];
      var last = models[^1];

      if (!string.Equals(first.Mnemonic, "ОК", StringComparison.OrdinalIgnoreCase))
      {
        first.Errors.Add(GeneralErrors.FirstCommandMustBeOk(first.StartLineNumber, $"{first.CommandNumber} {first.Mnemonic}"));
      }

      if (!string.Equals(last.Mnemonic, "КЦ", StringComparison.OrdinalIgnoreCase))
      {
        last.Errors.Add(GeneralErrors.LastCommandMustBeKc(last.StartLineNumber, $"{last.CommandNumber} {last.Mnemonic}"));
      }
    }

    /// <summary>
    /// Проверяет, что обязательные мнемоники присутствуют строго по одному разу.
    /// </summary>
    /// <param name="models">Список команд.</param>
    /// <remarks>
    /// Контролируются мнемоники:
    /// <c>ОК</c>, <c>РМ</c>, <c>СП</c>, <c>КЦ</c>.
    /// </remarks>
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

    /// <summary>
    /// Проверяет корректность меток переходов в командах УП.
    /// </summary>
    /// <param name="models">Список команд.</param>
    /// <remarks>
    /// Проверяется:
    /// <list type="bullet">
    /// <item><description>наличие метки;</description></item>
    /// <item><description>существование команды с указанным номером;</description></item>
    /// <item><description>числовой формат метки;</description></item>
    /// <item><description>числовой формат метки.</description></item>
    /// </list>
    /// </remarks>
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

        if (!int.TryParse(up.TargetLabel, out _))
        {
          up.Errors.Add(UpErrors.LabelIsNotNumber(up.TargetLabel, up.StartLineNumber, $"{up.CommandNumber} {up.Mnemonic}"));
          continue;
        }

      }
    }
  }
}
