using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains; // Для LoggerUtility
using ControlCommandAnalyser.Model.Ok;
using ControlCommandAnalyser.Parser.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Utilities;

namespace ControlCommandAnalyser.Parser.Si
{
  /// <summary>
  /// Парсер для команд СИ (сопротивление изоляции).
  /// </summary>
  public class SiCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "СИ";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LoggerUtility.LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new SiCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = lines?.ToList() ?? new List<string>(),
        StartLineNumber = numberLine,
      };

      if (lines == null || lines.Count == 0)
      {
        LoggerUtility.LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(SiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
      }

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

      string body = AllLinesInOne(model);

      // Дальше работаем ТОЛЬКО с body:
      var remainder = body;

      remainder = ManageSiParametersParse(model, commandNumber, mnemonic, numberLine, remainder);

      string bodyNoWs = string.Concat(lines.Select(l => Regex.Replace(l ?? string.Empty, @"\s+", "")));

      // Ищем первую и последнюю '*'
      int firstStar = bodyNoWs.IndexOf('*');
      int lastStar = bodyNoWs.LastIndexOf('*');

      if (firstStar >= 0 && lastStar > firstStar)
      {
        remainder = ParsePoints(commandNumber, mnemonic, numberLine, model, rmCommandModel, remainder, bodyNoWs, firstStar, lastStar);
      }
      else if (model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
      {
        // находим цепи точек из предыдущей команды проверки
        CommandsModel.CheckKeyP(model, model.Scheme);
      }
      else if (model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
      {
        CommandsModel.CheckKeyS(model.Scheme);
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

      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LoggerUtility.LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }

    private static string ParsePoints(string commandNumber, string mnemonic, int numberLine, SiCommandModel model, RmCommandModel rmCommandModel, string remainder, string bodyNoWs, int firstStar, int lastStar)
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
      if (model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
      {
        // находим цепи точек из предыдущей команды проверки
        CommandsModel.CheckKeyP(model, model.Scheme);
      }
      else if (model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
      {
        CommandsModel.CheckKeyS(model.Scheme);
      }
      return remainder;
    }

    public static string ManageSiParametersParse(SiCommandModel model, string commandNumber, string mnemonic, int numberLine, string remainder)
    {
      var body = remainder;
      var match = Regex.Match(remainder, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      if (match.Success)
        remainder = match.Groups[1].Value.Trim();

      string voltage = string.Empty, resistance = string.Empty, time = string.Empty;

      // сначала извлекаем ключи
      remainder = ExtractSiKeys(commandNumber, mnemonic, numberLine, model, body, remainder);

      remainder = ExtractSiParameters(commandNumber, mnemonic, numberLine, model, remainder, voltage, resistance, time);

      return remainder;
    }


    private static string ExtractSiParameters(string commandNumber, string mnemonic, int numberLine, SiCommandModel model, string remainder, string voltage, string resistance, string time)
    {
      (voltage, remainder) = CommonParameterParser.ParseVoltage(remainder);
      LoggerUtility.LogDebug($"После парсинга напряжения: voltage='{voltage}', remainder='{remainder}'");

      (resistance, remainder) = CommonParameterParser.ParseResistance(remainder);
      LoggerUtility.LogDebug($"После парсинга сопротивления: resistance='{resistance}', remainder='{remainder}'");

      (time, remainder) = CommonParameterParser.ParseTime(remainder);
      LoggerUtility.LogDebug($"После парсинга времени: time='{time}', remainder='{remainder}'");

      model.Voltage = voltage;
      if (string.IsNullOrEmpty(resistance))
      {
        resistance = "100<МОм";
      }
      model.Resistance = resistance;
      if (string.IsNullOrEmpty(time))
      {
        LoggerUtility.LogDebug($"Для времени установлено значение по умолчанию 5 с.'");
        time = "5с";
      }
      model.Time = time;
      ValidateSiParameters(commandNumber, mnemonic, numberLine, model, voltage, resistance, time);

      return remainder;
    }

    private static void ValidateSiParameters(string commandNumber, string mnemonic, int numberLine, SiCommandModel model, string? voltage, string? resistance, string time)
    {
      if (string.IsNullOrWhiteSpace(voltage))
      {
        model.Errors.Add(SiErrors.CannotParseParameters("Не указано напряжение", numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указано напряжение (строка {numberLine}): {commandNumber} {mnemonic}");
      }

      if (string.IsNullOrWhiteSpace(resistance))
      {
        model.Errors.Add(SiErrors.CannotParseParameters("Не указано сопротивление", numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указано сопротивление (строка {numberLine}): {commandNumber} {mnemonic}");
      }

      if (string.IsNullOrWhiteSpace(time))
      {
        model.Errors.Add(SiErrors.CannotParseParameters("Не указано время", numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указано время (строка {numberLine}): {commandNumber} {mnemonic}");
      }
    }

    private static string ExtractSiKeys(string commandNumber, string mnemonic, int numberLine, SiCommandModel model, string body, string remainder)
    {
      var result = AlgorithmKeyParser.ExtractKeysWithTrailingCommaCheck(body);

      foreach (var (key, hasError) in result)
      {
        if (hasError)
        {
          LoggerUtility.LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
          model.Errors.Add(SiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
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

      return remainder;
    }

    private static string AllLinesInOne(SiCommandModel model)
    {
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
      return body;
    }
  }
}