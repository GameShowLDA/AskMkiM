using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Менеджер разбора схем, шин и точек команд.
  /// Выполняет извлечение блока точек и формирует соответствующие модели.
  /// </summary>
  public static class SchemeManager
  {
    /// <summary>
    /// Извлекает и парсит схему из строки команды.
    /// </summary>
    public static SchemeModel GetScheme(BaseCommandModel model, RmCommandModel rmCommandModel, int numberLine, ref string remainder)
    {
      string bodyNoWs = Regex.Replace(remainder ?? string.Empty, @"\s+", "");
      string parseBody = ExtractDeletionList(bodyNoWs, out var deletionExpr, out var fullPointsSource);
      var scheme = new SchemeModel(new List<GroupModel>());
      if (model is PiCommandModel == false && model is PrCommandModel == false && model is SiCommandModel == false)
      {
        if (!TryExtractPointsBlock(parseBody, out var firstStar, out var lastStar))
        {
          HandleNoPointsBlock(model, numberLine);
          return null;
        }

        scheme = ParseScheme(model, parseBody, rmCommandModel, firstStar, lastStar, numberLine);
      }
      if (model is PiCommandModel piCommandModel)
      {
        piCommandModel = HandlePiCommandModel(parseBody, piCommandModel, numberLine, ref scheme, rmCommandModel);
        scheme = piCommandModel.Scheme;
      }
      if (model is SiCommandModel siCommandModel)
      {
        scheme = HandlePrCommandModel(parseBody, siCommandModel, siCommandModel.Scheme, numberLine, ref scheme, rmCommandModel);
      }
      if (model is PrCommandModel prCommandModel)
      {
        scheme = HandlePrCommandModel(parseBody, prCommandModel, prCommandModel.Scheme, numberLine, ref scheme, rmCommandModel);
      }

      scheme = ApplyDeletionList(model, scheme, rmCommandModel, numberLine, deletionExpr);
      ApplyAlgorithmWarnings(model, scheme);
      if (!string.IsNullOrEmpty(fullPointsSource))
        model.PointsSourse = fullPointsSource;

      remainder = ClearLineFromPoints(remainder);

      return scheme;
    }

    /// <summary>
    /// Обрабатывает схему для команды PI с учётом ключей алгоритма.
    /// </summary>
    private static PiCommandModel HandlePiCommandModel(string bodyNoWs, PiCommandModel piCommandModel, int numberLine, ref SchemeModel? scheme, RmCommandModel rmCommandModel)
    {
      if (TryExtractPointsBlock(bodyNoWs, out var firstStar, out var lastStar))
      {
        scheme = ParseScheme(piCommandModel, bodyNoWs, rmCommandModel, firstStar, lastStar, numberLine);
        piCommandModel.Scheme = scheme;
        piCommandModel = HandleKeysSP(numberLine, piCommandModel);
      }
      else if (piCommandModel.SiCommand.AlgorithmKey.Contains(AlgorithmKey.П.ToString())
        || piCommandModel.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
      {
        piCommandModel.Scheme = HandleKeyP(piCommandModel, piCommandModel.Scheme, numberLine, piCommandModel.SiCommand);
      }
      else if (piCommandModel.SiCommand.AlgorithmKey.Contains(AlgorithmKey.С.ToString())
        || piCommandModel.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
      {
        piCommandModel.Scheme = HandleKeyS(piCommandModel.Scheme, piCommandModel.SiCommand);
      }
      else
      {
        HandleNoPointsBlock(piCommandModel, numberLine);
      }
      return piCommandModel;
    }

    /// <summary>
    /// Обрабатывает схему для команд PR и SI.
    /// </summary>
    private static SchemeModel HandlePrCommandModel(string bodyNoWs, BaseCommandModel model, SchemeModel modelScheme, int numberLine, ref SchemeModel? scheme, RmCommandModel rmCommandModel)
    {
      if (TryExtractPointsBlock(bodyNoWs, out var firstStar, out var lastStar))
      {
        scheme = ParseScheme(model, bodyNoWs, rmCommandModel, firstStar, lastStar, numberLine);
        modelScheme = scheme;
        modelScheme = HandleKeysSP(numberLine,model, modelScheme);
      }
      else if (model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
      {
        modelScheme = HandleKeyP(model, modelScheme, numberLine);
      }
      else if (model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
      {
        modelScheme = HandleKeyS(modelScheme);
      }
      else
      {
        HandleNoPointsBlock(model, numberLine);
      }
      return modelScheme;
    }


    /// <summary>
    /// Применяет ключи алгоритма П или С для PI.
    /// </summary>
    private static PiCommandModel HandleKeysSP(int numberLine, PiCommandModel piCommandModel)
    {
      if (piCommandModel.SiCommand.AlgorithmKey.Contains(AlgorithmKey.П.ToString())
                  || piCommandModel.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
      {
        piCommandModel.Scheme = HandleKeyP(piCommandModel, piCommandModel.Scheme, numberLine, piCommandModel.SiCommand);
      }
      else if (piCommandModel.SiCommand.AlgorithmKey.Contains(AlgorithmKey.С.ToString())
        || piCommandModel.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
      {
        piCommandModel.Scheme = HandleKeyS(piCommandModel.Scheme, piCommandModel.SiCommand);
      }

      return piCommandModel;
    }

    /// <summary>
    /// Применяет ключи алгоритма П или С для базовой команды.
    /// </summary>
    private static SchemeModel HandleKeysSP(int numberLine, BaseCommandModel model, SchemeModel scheme)
    {
      if (model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
      {
        scheme = HandleKeyP(model, scheme, numberLine);
      }
      else if (model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
      {
        scheme = HandleKeyS(scheme);
      }

      return scheme;
    }

    /// <summary>
    /// Обрабатывает ключ алгоритма С.
    /// </summary>
    private static SchemeModel HandleKeyS(SchemeModel scheme, SiCommandModel siCommand = null)
    {
      scheme = CommandsModel.CheckKeyS(scheme);
      if (siCommand != null)
      {
        siCommand.Scheme = scheme;
      }
      return scheme;
    }

    /// <summary>
    /// Обрабатывает ключ алгоритма П.
    /// </summary>
    private static SchemeModel HandleKeyP(BaseCommandModel model, SchemeModel scheme, int numberLine, SiCommandModel siCommand = null)
    {
      var newScheme = new SchemeModel(new List<GroupModel>());
      if (siCommand != null)
      {
        newScheme = CommandsModel.CheckKeyP(model, scheme, siCommand);
      }
      else
      {
        newScheme = CommandsModel.CheckKeyP(model, scheme);
      }
      if (newScheme != null)
      {
        scheme = newScheme;
        if (siCommand != null)
        {
          siCommand.Scheme = scheme;
        }
      }
      else
      {
        model.Errors.Add(PiErrors.PreviousCommandHasNoPoints(numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
      }

      return scheme;
    }


    /// <summary>
    /// Возвращает список шин из блока точек.
    /// </summary>
    public static List<SwitchingBus> GetBusList(CkCommandModel model, RmCommandModel rmCommandModel, int numberLine, ref string remainder)
    {
      string bodyNoWs = Regex.Replace(remainder ?? string.Empty, @"\s+", "");

      if (!TryExtractPointsBlock(bodyNoWs, out var firstStar, out var lastStar))
      {
        return HandleNoBusBlock(model, numberLine);
      }
      model.BusList = ParseBusList(model, bodyNoWs, rmCommandModel, firstStar, lastStar);

      remainder = ClearLineFromPoints(remainder);

      return model.BusList;
    }

    /// <summary>
    /// Формирует словарь шин и соответствующих точек.
    /// </summary>
    public static Dictionary<SwitchingBus, List<PointModel>> GetBusPointsDictionary(BaseCommandModel model, RmCommandModel rmCommandModel, int numberLine,
      string commandNumber, string mnemonic, ref string remainder)
    {
      string bodyNoWs = Regex.Replace(remainder ?? string.Empty, @"\s+", "");

      if (!TryExtractPointsBlock(bodyNoWs, out var firstStar, out var lastStar))
      {
        if (model is OtCommandModel == false)
        {
          HandleNoPointsBlock(model, numberLine);
        }
        return new();
      }

      var pointsBlob = bodyNoWs.Substring(firstStar, lastStar - firstStar + 1);
      model.PointsSourse = pointsBlob;

      LogDebug($"Парсинг точек из общего блока: '{pointsBlob}'");

      var (busDictionary, pointErrors) =
          PointParser.ParseBusPoints(pointsBlob, rmCommandModel, numberLine, $"{commandNumber} {mnemonic}");

      CheckPointsErrors(model, numberLine, pointErrors);

      remainder = ClearLineFromPoints(remainder);

      return busDictionary ?? new();
    }


    /// <summary>
    /// Пытается найти границы блока точек '*...*'.
    /// </summary>
    private static bool TryExtractPointsBlock(string body, out int firstStar, out int lastStar)
    {
      firstStar = body.IndexOf('*');
      lastStar = body.LastIndexOf('*');

      return firstStar >= 0 && lastStar > firstStar;
    }

    /// <summary>
    /// Отделяет список исключения "~(...)*" от основного ССИРТ.
    /// </summary>
    private static string ExtractDeletionList(string body, out string deletionExpr, out string fullPointsSource)
    {
      deletionExpr = null;
      fullPointsSource = null;

      if (!TryExtractPointsBlock(body, out var firstStar, out var lastStar))
        return body;

      fullPointsSource = body.Substring(firstStar, lastStar - firstStar + 1);

      int deletionStart = body.IndexOf("~(", firstStar, StringComparison.Ordinal);
      if (deletionStart < 0 || deletionStart > lastStar)
        return body;

      int deletionEnd = body.IndexOf(')', deletionStart + 2);
      if (deletionEnd < 0 || deletionEnd > lastStar)
        return body;

      deletionExpr = body.Substring(deletionStart + 2, deletionEnd - deletionStart - 2);

      return body.Remove(deletionStart, lastStar - deletionStart + 1);
    }

    /// <summary>
    /// Обрабатывает ситуацию отсутствия блока точек.
    /// </summary>
    private static void HandleNoPointsBlock(BaseCommandModel model, int numberLine)
    {
      LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {model.CommandNumber} {model.Mnemonic}");

      model.Errors.Add(GeneralErrors.NoPointsBody(numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
    }

    /// <summary>
    /// Возвращает список шин по умолчанию при отсутствии блока.
    /// </summary>
    private static List<SwitchingBus> HandleNoBusBlock(CkCommandModel model, int numberLine)
    {
      LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {model.CommandNumber} {model.Mnemonic}");

      return Enum.GetValues<SwitchingBus>().Where(x => !x.ToString().StartsWith("AB")).ToList();
    }

    /// <summary>
    /// Парсит схему точек из блока.
    /// </summary>
    private static SchemeModel? ParseScheme(BaseCommandModel model, string body, RmCommandModel rmCommandModel,
      int firstStar, int lastStar, int numberLine)
    {
      var pointsBlob = body.Substring(firstStar, lastStar - firstStar + 1);
      model.PointsSourse = pointsBlob;

      LogDebug($"Парсинг точек из общего блока: '{pointsBlob}'");

      pointsBlob = ResolveReference(pointsBlob, model, CommandsModel.CommandModels, model.Errors);

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

    private static readonly Regex RefRegex = new(@"\^\d+");

    private static string ResolveReference(
    string expr,
    BaseCommandModel current,
    List<BaseCommandModel> allCommands,
    List<ErrorItem> errors)
    {
      if (string.IsNullOrWhiteSpace(expr))
        return expr;

      var match = RefRegex.Match(expr);
      if (!match.Success)
        return expr;

      int refNumber = int.Parse(match.Value.Substring(1));

      var referenced = FindReferencedCommand(refNumber, current, allCommands, errors);
      if (referenced == null)
        return expr;

      if (!ValidateReferencedCommand(referenced, errors))
        return expr;

      // ВАЖНО: берём ИСХОДНОЕ выражение ССИРТ
      var referencedExpr = referenced.PointsSourse;

      return expr.Replace(match.Value, referencedExpr);
    }

    private static BaseCommandModel FindReferencedCommand(
    int number,
    BaseCommandModel current,
    List<BaseCommandModel> allCommands,
    List<ErrorItem> errors)
    {
      var cmd = allCommands
          .Where(c => int.Parse(c.CommandNumber) == number)
          .OrderBy(c => c.StartLineNumber)
          .LastOrDefault(c => c.StartLineNumber < current.StartLineNumber);

      if (cmd == null)
      {
        errors.Add(new ErrorItem
        {
          Description = $"Команда с меткой {number} не найдена или находится ниже текущей.",
          Code = ErrorCode.Gen_InvalidReference
        });
        return null;
      }

      return cmd;
    }

    private static bool ValidateReferencedCommand(
    BaseCommandModel cmd,
    List<ErrorItem> errors)
    {
      // 1. ССИРТ должен быть
      if (string.IsNullOrWhiteSpace(cmd.PointsSourse))
      {
        AddRefError(errors, $"Команда {cmd.CommandNumber} не содержит ССИРТ.");
        return false;
      }

      // 2. Запрещена вложенная ссылка
      if (cmd.PointsSourse.Contains("^"))
      {
        AddRefError(errors,
            $"Команда {cmd.CommandNumber} содержит ссылку на ССИРТ (цепочка ссылок запрещена).");
        return false;
      }

      // 3. Запрещён '~'
      if (cmd.PointsSourse.StartsWith("~"))
      {
        AddRefError(errors,
            $"Команда {cmd.CommandNumber} содержит исключения (~), нельзя использовать в ссылке.");
        return false;
      }

      // 4. Проверка успешного разбора
      if (cmd.Errors.Count>0) // или аналогичное поле
      {
        AddRefError(errors,
            $"Команда {cmd.CommandNumber} содержит ошибки и не может использоваться как ссылка.");
        return false;
      }

      return true;
    }

    private static void AddRefError(List<ErrorItem> errors, string message)
    {
      errors.Add(new ErrorItem
      {
        Description = message,
        Code = ErrorCode.Gen_InvalidReference
      });
    private static SchemeModel ApplyDeletionList(BaseCommandModel model, SchemeModel scheme, RmCommandModel rmCommandModel, int numberLine, string deletionExpr)
    {
      if (scheme == null || string.IsNullOrWhiteSpace(deletionExpr))
        return scheme;

      var (deletionPoints, deletionErrors) = PointParser.ParseDeletionPoints(deletionExpr, model, rmCommandModel);
      CheckPointsErrors(model, numberLine, deletionErrors);

      if (deletionErrors.Count > 0 || deletionPoints.Count == 0)
        return scheme;

      var deletionAddresses = deletionPoints
        .Select(point => point.ToString())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

      var filteredGroups = scheme.GroupModels
        .Where(group => !GroupContainsAnyPoint(group, deletionAddresses))
        .ToList();

      return new SchemeModel(filteredGroups);
    }

    private static bool GroupContainsAnyPoint(GroupModel group, HashSet<string> deletionAddresses)
    {
      if (group?.ChainModels == null)
        return false;

      foreach (var chain in group.ChainModels)
      {
        if (chain?.PointModels == null)
          continue;

        foreach (var point in chain.PointModels)
        {
          if (point != null && deletionAddresses.Contains(point.ToString()))
            return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Для ПР отключает проверку разобщения, если после всех преобразований осталось меньше двух разобщенных цепей.
    /// </summary>
    private static void ApplyAlgorithmWarnings(BaseCommandModel model, SchemeModel scheme)
    {
      if (scheme?.GroupModels == null || scheme.GroupModels.Count == 0)
        return;

      if (model.Mnemonic != EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PR).DisplayName)
        return;

      int disconnectedCount = 0;
      foreach (var group in scheme.GroupModels)
      {
        if (scheme.GetPointsDisconnected(group) != null)
          disconnectedCount++;
      }

      if (disconnectedCount < 2 &&
          disconnectedCount != 0 &&
          !model.AlgorithmKey.Contains(AlgorithmKey.ЗР.ToString()))
      {
        model.AlgorithmKey.Add(AlgorithmKey.ЗР.ToString());
        model.Warnings.Add(GeneralWarnings.KeyZR(model.StartLineNumber, $"{model.CommandNumber} {model.Mnemonic}"));
      }
    }

    /// <summary>
    /// Парсит список шин из блока.
    /// </summary>
    private static List<SwitchingBus> ParseBusList(CkCommandModel model, string body, RmCommandModel rmCommandModel, int firstStar, int lastStar)
    {
      var busBlob = body.Substring(firstStar, lastStar - firstStar + 1);

      LogDebug($"Парсинг шин из общего блока: '{busBlob}'");

      var busList = PointParser.ParseBusList(busBlob);

      if (busList.Count > 0)
      {
        LogInformation($"Шины распознаны: количество = {model.BusList.Count}");
        return busList;
      }
      else
      {
        return Enum.GetValues<SwitchingBus>().Where(x => !x.ToString().StartsWith("AB")).ToList();
      }
    }

    /// <summary>
    /// Обрабатывает ситуацию пустой схемы.
    /// </summary>
    private static void HandleEmptyScheme(BaseCommandModel model, int numberLine)
    {
      LogWarning(
          $"Не найдено ни одной точки (строка {numberLine}): {model.CommandNumber} {model.Mnemonic}");

      model.Errors.Add(
          GeneralErrors.EmptyPointsBody(numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
    }

    /// <summary>
    /// Добавляет ошибки, возникшие при разборе точек.
    /// </summary>
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

    /// <summary>
    /// Удаляет блок точек из строки команды.
    /// </summary>
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

