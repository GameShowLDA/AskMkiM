using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text.RegularExpressions;

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

      if (HasCriticalStructuralErrors(models))
        return;

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
      var rmModel = models.OfType<RmCommandModel>().FirstOrDefault();
      if (rmModel == null)
        return;

      var duplicateDestinations = pointsMap
        .GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
        .Where(g => g.Count() > 1)
        .Select(g => g.Key)
        .Where(point => !rmModel.ReportedDuplicateDestinationPoints.Contains(point))
        .ToList();

      foreach (var group in GroupConsecutivePoints(duplicateDestinations))
      {
        if (group.Count == 1)
        {
          rmModel.Errors.Add(GeneralErrors.DuplicateDestinationPoint(
            group[0],
            rmModel.StartLineNumber,
            $"{rmModel.CommandNumber} {rmModel.Mnemonic}"));
          continue;
        }

        var (firstPoint, lastPoint) = OrderRangeEndpoints(group[0], group[^1]);
        rmModel.Errors.Add(GeneralErrors.DuplicateDestinationPoints(
          firstPoint,
          lastPoint,
          rmModel.StartLineNumber,
          $"{rmModel.CommandNumber} {rmModel.Mnemonic}",
          ResolveSourceFragment(rmModel.PointsSourse, group)));
      }
    }

    private static bool HasCriticalStructuralErrors(IEnumerable<BaseCommandModel> models)
    {
      return models.Any(model =>
        model.Errors?.Any(CriticalTranslationErrorClassifier.IsCriticalStructural) == true);
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
    public static (List<ErrorItem>, List<PointModel>) GetPointsModel(List<string> points, BaseCommandModel model, RmCommandModel rmCommandModel)
    {
      List<PointModel> pointModels = new List<PointModel>();
      List<ErrorItem> error = new();

      foreach (var point in points)
      {
        if (!rmCommandModel.TryGetAddressByKey(point, out string pointPart))
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
    /// Объединяет ошибки соседних отсутствующих точек в диапазоны.
    /// Несмежные точки остаются отдельными диагностическими сообщениями.
    /// </summary>
    internal static void CollapseUnknownPointErrors(
      List<ErrorItem> errors,
      string? sourceExpression)
    {
      var unknownErrors = errors
        .Select((error, index) => (Error: error, Index: index))
        .Where(item => item.Error.Code == ErrorCode.Gen_UnknownPoint
          && !string.IsNullOrWhiteSpace(item.Error.SourceFragment))
        .ToList();

      if (unknownErrors.Count == 0)
        return;

      var pointGroups = GroupConsecutivePoints(unknownErrors
        .Select(item => item.Error.SourceFragment!)
        .ToList());

      if (pointGroups.Count == 0)
        return;

      var firstError = unknownErrors[0].Error;
      var replacements = pointGroups
        .Select(group => CreateUnknownPointError(group, firstError, sourceExpression))
        .ToList();

      int insertionIndex = unknownErrors[0].Index;
      var collapsedErrors = unknownErrors
        .Select(item => item.Error)
        .ToHashSet();
      errors.RemoveAll(collapsedErrors.Contains);
      errors.InsertRange(Math.Min(insertionIndex, errors.Count), replacements);
    }

    /// <summary>
    /// Объединяет одинаковые ошибки конфигурации модуля для соседних точек
    /// шинных команд ОТ/ПТ.
    /// </summary>
    internal static void CollapseBusPointErrors(
      List<ErrorItem> errors,
      string? sourceExpression)
    {
      var busErrors = errors
        .Select((error, index) => (Error: error, Index: index))
        .Where(item => item.Error.Code == ErrorCode.Gen_BusPointConfiguration
          && !string.IsNullOrWhiteSpace(item.Error.SourceFragment))
        .ToList();

      if (busErrors.Count == 0)
        return;

      var groups = GroupConsecutiveBusErrors(
        busErrors.Select(item => item.Error).ToList());
      var replacements = groups
        .Select(group => CreateBusPointError(group, sourceExpression))
        .ToList();

      int insertionIndex = busErrors[0].Index;
      var collapsedErrors = busErrors
        .Select(item => item.Error)
        .ToHashSet();
      errors.RemoveAll(collapsedErrors.Contains);
      errors.InsertRange(Math.Min(insertionIndex, errors.Count), replacements);
    }

    private static List<List<ErrorItem>> GroupConsecutiveBusErrors(
      IReadOnlyList<ErrorItem> errors)
    {
      var result = new List<List<ErrorItem>>();
      if (errors.Count == 0)
        return result;

      var current = new List<ErrorItem> { errors[0] };
      int direction = 0;
      for (int i = 1; i < errors.Count; i++)
      {
        var previous = current[^1];
        var next = errors[i];
        string previousPrefix = string.Empty;
        string nextPrefix = string.Empty;
        int previousNumber = 0;
        int nextNumber = 0;
        bool consecutive = string.Equals(
            previous.Description,
            next.Description,
            StringComparison.Ordinal)
          && TryGetPointNumber(
            previous.SourceFragment!, out previousPrefix, out previousNumber)
          && TryGetPointNumber(
            next.SourceFragment!, out nextPrefix, out nextNumber)
          && string.Equals(previousPrefix, nextPrefix, StringComparison.OrdinalIgnoreCase)
          && Math.Abs(nextNumber - previousNumber) == 1;
        int nextDirection = consecutive
          ? Math.Sign(nextNumber - previousNumber)
          : 0;

        if (consecutive && (direction == 0 || direction == nextDirection))
        {
          current.Add(next);
          direction = nextDirection;
          continue;
        }

        result.Add(current);
        current = new List<ErrorItem> { next };
        direction = 0;
      }

      result.Add(current);
      return result;
    }

    private static ErrorItem CreateBusPointError(
      IReadOnlyList<ErrorItem> errors,
      string? sourceExpression)
    {
      if (errors.Count == 1)
        return errors[0];

      var template = errors[0];
      var points = errors.Select(error => error.SourceFragment!).ToList();
      var (firstPoint, lastPoint) = OrderRangeEndpoints(points[0], points[^1]);
      string sourceFragment = ResolveSourceFragment(sourceExpression, points);

      return GeneralErrors.BusPointsConfiguration(
        template.Description,
        firstPoint,
        lastPoint,
        template.SourceLineNumber,
        template.Command,
        sourceFragment);
    }

    private static ErrorItem CreateUnknownPointError(
      IReadOnlyList<string> points,
      ErrorItem template,
      string? sourceExpression)
    {
      string sourceFragment = ResolveSourceFragment(sourceExpression, points);
      if (points.Count == 1)
      {
        var error = GeneralErrors.UnknownPoint(
          points[0], template.SourceLineNumber, template.Command);
        error.SourceFragment = sourceFragment;
        return error;
      }

      var (firstPoint, lastPoint) = OrderRangeEndpoints(points[0], points[^1]);
      return GeneralErrors.UnknownPoints(
        firstPoint,
        lastPoint,
        template.SourceLineNumber,
        template.Command,
        sourceFragment);
    }

    private static List<List<string>> GroupConsecutivePoints(IReadOnlyList<string> points)
    {
      var result = new List<List<string>>();
      if (points.Count == 0)
        return result;

      var current = new List<string> { points[0] };
      int direction = 0;

      for (int i = 1; i < points.Count; i++)
      {
        bool previousParsed = TryGetPointNumber(
          current[^1], out string previousPrefix, out int previousNumber);
        bool currentParsed = TryGetPointNumber(
          points[i], out string currentPrefix, out int currentNumber);
        bool consecutive = previousParsed
          && currentParsed
          && string.Equals(previousPrefix, currentPrefix, StringComparison.OrdinalIgnoreCase)
          && Math.Abs(currentNumber - previousNumber) == 1;

        int nextDirection = consecutive
          ? Math.Sign(currentNumber - previousNumber)
          : 0;

        if (consecutive && (direction == 0 || direction == nextDirection))
        {
          current.Add(points[i]);
          direction = nextDirection;
          continue;
        }

        result.Add(current);
        current = new List<string> { points[i] };
        direction = 0;
      }

      result.Add(current);
      return result;
    }

    private static (string First, string Last) OrderRangeEndpoints(
      string firstPoint,
      string lastPoint)
    {
      if (TryGetPointNumber(firstPoint, out string firstPrefix, out int firstNumber)
        && TryGetPointNumber(lastPoint, out string lastPrefix, out int lastNumber)
        && string.Equals(firstPrefix, lastPrefix, StringComparison.OrdinalIgnoreCase)
        && firstNumber > lastNumber)
      {
        return (lastPoint, firstPoint);
      }

      return (firstPoint, lastPoint);
    }

    private static string ResolveSourceFragment(
      string? sourceExpression,
      IReadOnlyList<string> points)
    {
      if (string.IsNullOrWhiteSpace(sourceExpression))
        return points.Count == 1 ? points[0] : $"{points[0]}-{points[^1]}";

      foreach (Match match in Regex.Matches(
        sourceExpression,
        @"[^\s,*#=~()]+-\*?[^\s,*#=~()]+"))
      {
        if (RangeContainsPoints(match.Value, points, out string matchingFragment))
          return matchingFragment;
      }

      int firstIndex = sourceExpression.IndexOf(points[0], StringComparison.OrdinalIgnoreCase);
      int lastIndex = sourceExpression.IndexOf(points[^1], StringComparison.OrdinalIgnoreCase);
      if (firstIndex >= 0 && lastIndex >= 0)
      {
        int start = Math.Min(firstIndex, lastIndex);
        int end = Math.Max(firstIndex + points[0].Length, lastIndex + points[^1].Length);
        return sourceExpression[start..end];
      }

      return sourceExpression.Trim().Trim('*');
    }

    private static bool RangeContainsPoints(
      string candidate,
      IReadOnlyList<string> points,
      out string matchingFragment)
    {
      if (RangeContainsPointsCore(candidate, points))
      {
        matchingFragment = candidate;
        return true;
      }

      int colonIndex = candidate.IndexOf(':');
      if (colonIndex >= 0)
      {
        string tail = candidate[(colonIndex + 1)..];
        if (RangeContainsPointsCore(tail, points))
        {
          matchingFragment = tail;
          return true;
        }
      }

      matchingFragment = string.Empty;
      return false;
    }

    private static bool RangeContainsPointsCore(
      string candidate,
      IReadOnlyList<string> points)
    {
      string normalized = candidate.Replace("-*", "-", StringComparison.Ordinal);
      int dashIndex = normalized.IndexOf('-');
      if (dashIndex <= 0 || dashIndex >= normalized.Length - 1)
        return false;

      string left = normalized[..dashIndex];
      string right = normalized[(dashIndex + 1)..];
      if (!TryGetPointNumber(left, out string leftPrefix, out int start)
        || !TryGetPointNumber(right, out string rightPrefix, out int end))
      {
        return false;
      }

      if (string.IsNullOrEmpty(rightPrefix))
        rightPrefix = leftPrefix;

      if (!string.Equals(leftPrefix, rightPrefix, StringComparison.OrdinalIgnoreCase))
        return false;

      int min = Math.Min(start, end);
      int max = Math.Max(start, end);
      foreach (string point in points)
      {
        if (!TryGetPointNumber(point, out string pointPrefix, out int pointNumber)
          || !string.Equals(pointPrefix, leftPrefix, StringComparison.OrdinalIgnoreCase)
          || pointNumber < min
          || pointNumber > max)
        {
          return false;
        }
      }

      return true;
    }

    private static bool TryGetPointNumber(
      string point,
      out string prefix,
      out int number)
    {
      prefix = string.Empty;
      number = 0;
      var match = Regex.Match(point ?? string.Empty, @"^(.*?)(\d+)$");
      if (!match.Success || !int.TryParse(match.Groups[2].Value, out number))
        return false;

      prefix = match.Groups[1].Value;
      return true;
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
