using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using ControlCommandAnalyser.Parser.Common;
using ControlCommandAnalyser.Parser.Si; // Для LoggerUtility
using System.Text.RegularExpressions;
using Utilities;

namespace ControlCommandAnalyser.Parser.Pi
{
  /// <summary>
  /// Парсер для команды ПИ (пробой изоляции).
  /// </summary>
  public class PiCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "ПИ";

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

      var model = new PiCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      if (lines == null || lines.Count == 0)
      {
        LoggerUtility.LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(PiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      // Убираем полностью пустые/пробельные строки (чтобы не таскать мусор)
      model.SourceLines = model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

      // Склеиваем всё в одну строку и удаляем \r \n \t
      var body = string.Concat(model.SourceLines)
        .Replace("\r", "")
        .Replace("\n", "");

      // Приведи ввод к «чистому» виду (NBSP, латиница->кириллица и т.п.)
      body = PiSiSplitter.PreNormalize(body);

      // Вырезаем номер + "ПИ" заранее, чтобы в сплит не летел заголовок
      var head = Regex.Match(body, @"^\s*\d+\s+ПИ\s*(.*)$", RegexOptions.IgnoreCase);
      var remainder = head.Success ? head.Groups[1].Value : body;

      LoggerUtility.LogDebug($"Хвост после ПИ: \"{remainder}\"");
      var (siPart, piPart, errs) = PiSiSplitter.SplitSiFromPiStrict(remainder);
      if (errs.Count > 0)
      {
        LoggerUtility.LogWarning($"Strict WS issues: {string.Join(" | ", errs)}");
      }

      var modelSi = new SiCommandModel();
      var siRemainder = SiCommandParser.ManageSiParametersParse(modelSi, commandNumber, mnemonic, numberLine, siPart);

      // Если СИ что-то не допарсила, логни отдельно (в модель СИ, не ПИ)
      if (!string.IsNullOrWhiteSpace(siRemainder))
      {
        LoggerUtility.LogWarning($"Не распознано в СИ: '{siRemainder}'");
      }
      model.SiCommand = modelSi;

      var remainderPi = piPart;

      // Если у тебя в ПИ могут ещё раз встречаться номер+мнемоника (на всякий), можно безопасно срезать:
      var match2 = Regex.Match(remainderPi, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      if (match2.Success) remainderPi = match2.Groups[1].Value.Trim();

      // --- парсим ПИ только из remainderPi ---
      string? voltage = null, time = null;

      var result = AlgorithmKeyParser.ExtractKeysWithTrailingCommaCheck(remainderPi);
      foreach (var (key, hasError) in result)
      {
        if (hasError)
        {
          model.Errors.Add(PiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        }
        else
        {
          var t1 = AlgorithmKey.Т1.ToString();
          if (!modelSi.AlgorithmKey.Contains(t1) && !string.IsNullOrEmpty(key))
          {
            model.AlgorithmKey.Add(key);
            LoggerUtility.LogDebug($"Найден ключ алгоритма: {key}");
          }
          else
          {
            model.Errors.Add(PiErrors.KeysConflict(numberLine, $"{commandNumber} {mnemonic}"));
            LoggerUtility.LogWarning($"Команда ПИ не может содержать ключ Г, если для команды СИ присвоен ключ Т1: " +
              $"{commandNumber} {mnemonic} (строка {numberLine})");
          }
        }
      }

      // удаляем найденные ключи ТОЛЬКО из ПИ-остатка
      foreach (var key in model.AlgorithmKey)
        remainderPi = remainderPi.Replace(key, "", StringComparison.OrdinalIgnoreCase);

      // Парсим параметры
      (voltage, remainderPi) = CommonParameterParser.ParseVoltage(remainderPi);
      LoggerUtility.LogDebug($"После парсинга напряжения: voltage='{voltage}', remainder='{remainderPi}'");

      (time, remainderPi) = CommonParameterParser.ParseTime(remainderPi);
      LoggerUtility.LogDebug($"После парсинга времени: time='{time}', remainder='{remainderPi}'");

      if (remainderPi.Contains('+'))
      {
        model.VoltageType = VoltageEnum.Type.DCW;
        remainderPi = remainderPi.Replace("+", string.Empty);
      }

      model.Voltage = voltage;
      model.Time = string.IsNullOrEmpty(time) ? "1c" : time;

      if (string.IsNullOrWhiteSpace(voltage))
      {
        model.Errors.Add(PiErrors.CannotParseParameters("Не указано напряжение", numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указано напряжение (строка {numberLine}): {commandNumber} {mnemonic}");
      }

      if (string.IsNullOrWhiteSpace(time))
      {
        model.Errors.Add(PiErrors.CannotParseParameters("Не указано время", numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указано время (строка {numberLine}): {commandNumber} {mnemonic}");
      }

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
        int idxStarInFirstLine = remainderPi.IndexOf('*');
        remainderPi = idxStarInFirstLine >= 0 ? remainderPi[..idxStarInFirstLine].Trim() : remainderPi.Trim();
        if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.П.ToString())
          || model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
        {
          // находим цепи точек из предыдущей команды проверки
          model.Scheme = CommandsModel.CheckKeyP(model, model.Scheme, model.SiCommand);
          model.SiCommand.Scheme = model.Scheme;
        }
        else if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.С.ToString())
          || model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
        {
          model.Scheme = CommandsModel.CheckKeyS(model.Scheme);
          model.SiCommand.Scheme = model.Scheme;
        }
      }
      else if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.П.ToString())
        || model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
      {
        // находим цепи точек из предыдущей команды проверки
        model.Scheme = CommandsModel.CheckKeyP(model.SiCommand, model.Scheme);
        model.SiCommand.Scheme = model.Scheme;
      }
      else if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.С.ToString())
        || model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
      {
        model.Scheme = CommandsModel.CheckKeyS(model.Scheme);
        model.SiCommand.Scheme = model.Scheme;
      }
      else
      {
        // Во всём теле команды не нашли пары '*...*' → считаем, что точек нет
        LoggerUtility.LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {commandNumber} {mnemonic}");
        model.Errors.Add(IeErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }

      CheckUnparsedParameters(commandNumber, mnemonic, numberLine, model, remainderPi);

      LoggerUtility.LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }

    private static void CheckUnparsedParameters(string commandNumber, string mnemonic, int numberLine, PiCommandModel model, string remainderPi)
    {
      if (!string.IsNullOrEmpty(remainderPi))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainderPi;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainderPi, numberLine, $"{commandNumber} {mnemonic}"));
      }
    }
  }
}
