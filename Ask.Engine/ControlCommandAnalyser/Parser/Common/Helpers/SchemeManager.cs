using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
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
      var scheme = new SchemeModel(new List<GroupModel>());
      if (model is PiCommandModel == false && model is PrCommandModel == false && model is SiCommandModel == false)
      {
        if (!TryExtractPointsBlock(bodyNoWs, out var firstStar, out var lastStar))
        {
          HandleNoPointsBlock(model, numberLine);
          return null;
        }

        scheme = ParseScheme(model, bodyNoWs, rmCommandModel, firstStar, lastStar, numberLine);
      }
      if (model is PiCommandModel piCommandModel)
      {
        piCommandModel = HandlePiCommandModel(bodyNoWs, piCommandModel, numberLine, ref scheme, rmCommandModel);
        scheme = piCommandModel.Scheme;
      }
      if (model is SiCommandModel siCommandModel)
      {
        scheme = HandlePrCommandModel(bodyNoWs, siCommandModel, siCommandModel.Scheme, numberLine, ref scheme, rmCommandModel);
      }
      if (model is PrCommandModel prCommandModel)
      {
        scheme = HandlePrCommandModel(bodyNoWs, prCommandModel, prCommandModel.Scheme, numberLine, ref scheme, rmCommandModel);
      }

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
    /// Обрабатывает ситуацию отсутствия блока точек.
    /// </summary>
    private static void HandleNoPointsBlock(BaseCommandModel model, int numberLine)
    {
      LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {model.CommandNumber} {model.Mnemonic}");

      model.Errors.Add(EhtErrors.EmptyPoints(numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
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
          EhtErrors.EmptyPoints(numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
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
