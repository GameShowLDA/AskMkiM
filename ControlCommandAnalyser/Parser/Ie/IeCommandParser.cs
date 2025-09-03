using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using ControlCommandAnalyser.Parser.Common;
using System.Text.RegularExpressions;
using Utilities;

namespace ControlCommandAnalyser.Parser.Ie
{
  /// <summary>
  /// Парсер для команд ИЕ (измерение емкости).
  /// </summary>
  [AllowedKeys( AlgorithmKey.Д)]
  internal class IeCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "ИЕ";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LoggerUtility.LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var rmCommandModel = new RmCommandModel();
      var commandModel = CommandsModel.GetByMnemonic("РМ").Last();

      if (commandModel == null)
      {
        throw new Exception("РМ не сущесвует...");
      }
      else
      {
        if (commandModel is RmCommandModel)
        {
          rmCommandModel = commandModel as RmCommandModel;
        }
        else
        {
          throw new Exception("РМ не сущесвует...");
        }
      }

      var model = new IeCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      if (lines == null || lines.Count == 0)
      {
        LoggerUtility.LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(IeErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      var errors = IndentationCheker.CheckIndentationErrors(lines, commandNumber, mnemonic);
      if (errors.Count > 0)
      {
        foreach (var error in errors)
        {
          LoggerUtility.LogError(error);
          model.Errors.Add(GeneralErrors.IndentationError(mnemonic, numberLine, $"{commandNumber} {mnemonic}"));
          return model;
        }
      }

      // Убираем полностью пустые/пробельные строки (чтобы не таскать мусор)
      model.SourceLines = model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

      // Склеиваем всё в одну строку и удаляем \r \n \t
      var body = string.Concat(model.SourceLines)
        .Replace("\r", "")
        .Replace("\n", "")
        .Replace("\t", "");

      // Для логов
      LoggerUtility.LogDebug($"Нормализованное тело команды (в одну строку): \"{body}\"");

      // Дальше работаем ТОЛЬКО с body:
      var remainder = body;

      var match = Regex.Match(remainder, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      if (match.Success)
        remainder = match.Groups[1].Value.Trim();

      string? lowerLimitCapacity = null, higherLimitCapacity = null, unit = null;

      var result = AlgorithmKeyParser.ExtractKeysWithTrailingCommaCheck(remainder);

      foreach (var (key, hasError) in result)
      {
        if (hasError)
        {
          LoggerUtility.LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
          model.Errors.Add(IeErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        }
        else
        {
          model.AlgorithmKey.Add(key);
          LoggerUtility.LogDebug($"Найден ключ алгоритма: {key}");
        }
      }

      // затем удаляем их из строки
      foreach (var key in model.AlgorithmKey)
      {
        remainder = remainder.Replace(key, "", StringComparison.OrdinalIgnoreCase);
      }

      (lowerLimitCapacity, higherLimitCapacity, unit, remainder) = CommonParameterParser.ParseCapacityRange(remainder);
      LoggerUtility.LogDebug($"После парсинга напряжения: нижняя граница электрической емкости='{lowerLimitCapacity}',верхняя граница электрической емкости='{higherLimitCapacity}', единица измерения = '{unit}' remainder='{remainder}'");

      if (string.IsNullOrEmpty(lowerLimitCapacity) && string.IsNullOrEmpty(higherLimitCapacity))
      {
        model.Errors.Add(IeErrors.EmptyCapacity(numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указана электрическая емкость (строка {numberLine}): {commandNumber} {mnemonic}");
        if (!string.IsNullOrEmpty(remainder))
        {
          model.UnparsedParameters = "! Не распознанные параметры: ";
          model.UnparsedParameters += remainder;
          model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
        }
        return model;
      }

      model.LowerLimitCapacity = lowerLimitCapacity;
      model.HigherLimitCapacity = higherLimitCapacity;

      if (!string.IsNullOrWhiteSpace(model.LowerLimitCapacity))
      {
        model.LowerLimitCapacity += " " + unit;
      }

      if (!string.IsNullOrWhiteSpace(model.HigherLimitCapacity))
      {
        model.HigherLimitCapacity += " " + unit;
      }

      if (HasInvalidParameterOrder(body, model.AlgorithmKey, lowerLimitCapacity ?? higherLimitCapacity, out string err))
      {
        model.Errors.Add(GeneralErrors.InvalidParameterOrder(mnemonic, numberLine, $"{commandNumber} {mnemonic}", err));
        LoggerUtility.LogWarning($"Ошибка порядка параметров (строка {numberLine}): {err}");
        return model;
      }

      //var schemeModel = new SchemeModel(new List<ChainModel>());
      // --- новый разбор блока точек между первой и последней '*' во всём теле команды ---
      // Собираем всё тело команды (включая последующие строки), убираем все пробельные символы
      string bodyNoWs = string.Concat(lines.Select(l => Regex.Replace(l ?? string.Empty, @"\s+", "")));

      // Ищем первую и последнюю '*'
      int firstStar = bodyNoWs.IndexOf('*');
      int lastStar = bodyNoWs.LastIndexOf('*');

      if (firstStar >= 0 && lastStar > firstStar)
      {
        // Выделяем блок точек (включительно) — PointParser сам Trim('*')
        string pointsBlob = bodyNoWs.Substring(firstStar, lastStar - firstStar + 1);
        LoggerUtility.LogDebug($"Парсинг точек из общего блока: '{pointsBlob}'");

        var (scheme, pointErrors) = PointParser.ParsePoints(pointsBlob, mnemonic, rmCommandModel);

        // Поднимем ошибки парсера точек
        if (pointErrors?.Count > 0)
        {
          foreach (var error in pointErrors)
          {
            error.SourceLineNumber = numberLine;
            error.Command = $"{commandNumber} {mnemonic}";
            model.Errors.Add(error);
            LoggerUtility.LogError(
              $"При парсинге точек команды {commandNumber} {mnemonic} произошла ошибка: {error.Description} (строка {error.SourceLineNumber}).");
          }
        }

        // Проверим, что схема непуста (есть хотя бы одна точка)
        if (scheme == null || scheme.IsEmpty())
        {
          LoggerUtility.LogWarning($"Не найдено ни одной точки (строка {numberLine}): {commandNumber} {mnemonic}");
          model.Errors.Add(IeErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
        }
        else
        {
          model.Scheme = scheme; // ← просто присваиваем схему в модель
          LoggerUtility.LogInformation(
            $"Схема распознана: цепей={scheme.GroupModels?.Count ?? 0}, частей={scheme.CountParts()}, точек={scheme.CountPoints()}");
        }

        // Обновим remainder: оставим в нём только то, что до первой '*' в ПЕРВОЙ строке
        int idxStarInFirstLine = remainder.IndexOf('*');
        remainder = idxStarInFirstLine >= 0 ? remainder[..idxStarInFirstLine].Trim() : remainder.Trim();
      }
      else
      {
        // Во всём теле команды не нашли пары '*...*' → считаем, что точек нет
        LoggerUtility.LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {commandNumber} {mnemonic}");
        model.Errors.Add(IeErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }


      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
      }

      // Валидация
      if (string.IsNullOrWhiteSpace(lowerLimitCapacity) && string.IsNullOrWhiteSpace(higherLimitCapacity))
      {
        LoggerUtility.LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(IeErrors.CannotParseParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
      }

      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LoggerUtility.LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }

    public static bool HasInvalidParameterOrder(string firstLine, List<string> algorithmKeys, string? resistanceStart, out string errorDescription)
    {
      errorDescription = string.Empty;

      int idxKey = -1;
      int idxResistance = -1;
      int idxPoint = firstLine.IndexOf('*');

      // Позиция первого ключа
      foreach (var key in algorithmKeys)
      {
        int idx = firstLine.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0 && (idxKey == -1 || idx < idxKey))
          idxKey = idx;
      }

      // Сопротивление
      if (!string.IsNullOrWhiteSpace(resistanceStart))
      {
        idxResistance = firstLine.IndexOf(resistanceStart, StringComparison.OrdinalIgnoreCase);
      }

      // - Ключ должен идти до сопротивления
      if (idxKey != -1 && idxResistance != -1 && idxKey > idxResistance)
      {
        errorDescription = "Ключ алгоритма указан после электрической емкости.";
        return true;
      }

      // - Все параметры должны быть до точек
      if (idxPoint != -1)
      {
        if ((idxKey != -1 && idxKey > idxPoint)
         || (idxResistance != -1 && idxResistance > idxPoint))
        {
          errorDescription = "Один из параметров указан после точек.";
          return true;
        }
      }

      return false;
    }
  }
}
