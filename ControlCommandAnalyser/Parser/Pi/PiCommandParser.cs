using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Formatter;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using ControlCommandAnalyser.Parser.Common;
using ControlCommandAnalyser.Parser.Si; // Для LoggerUtility
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Utilities;
using Utilities.Models;

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
        .Replace("\n", "")
        .Replace("\t", "");

      // Для логов
      LoggerUtility.LogDebug($"Нормализованное тело команды (в одну строку): \"{body}\"");

      // Дальше работаем ТОЛЬКО с body:
      var remainder = body;
      var splitReminder = remainder.Split("  ");
      remainder = splitReminder[2];

      var modelSi = new SiCommandModel();
      var lastString = SiCommandParser.ManageSiParametersParse(modelSi, commandNumber, mnemonic, numberLine, splitReminder[1]);
      CheckUnparsedParameters(commandNumber, mnemonic, numberLine, model, lastString);
      model.SiCommand = modelSi;

      var match = Regex.Match(remainder, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      if (match.Success)
        remainder = match.Groups[1].Value.Trim();

      string? voltage = null, time = null;

      var result = AlgorithmKeyParser.ExtractKeysWithTrailingCommaCheck(remainder);

      foreach (var (key, hasError) in result)
      {
        if (hasError)
        {
          LoggerUtility.LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
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

      // затем удаляем их из строки
      foreach (var key in model.AlgorithmKey)
      {
        remainder = remainder.Replace(key, "", StringComparison.OrdinalIgnoreCase);
      }

      // Парсим параметры
      (voltage, remainder) = CommonParameterParser.ParseVoltage(remainder);
      LoggerUtility.LogDebug($"После парсинга напряжения: voltage='{voltage}', remainder='{remainder}'");

      (time, remainder) = CommonParameterParser.ParseTime(remainder);
      LoggerUtility.LogDebug($"После парсинга времени: time='{time}', remainder='{remainder}'");

      if (remainder.Contains('+'))
      {
        model.VoltageType = VoltageEnum.Type.DCW;
        remainder = remainder.Replace("+", string.Empty);
      }

      model.Voltage = voltage;

      if (string.IsNullOrEmpty(time))
      {
        time = "1c";
      }

      model.Time = time;

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
        int idxStarInFirstLine = remainder.IndexOf('*');
        remainder = idxStarInFirstLine >= 0 ? remainder[..idxStarInFirstLine].Trim() : remainder.Trim();
        if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.П.ToString()) 
          || model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
        {
          // находим цепи точек из предыдущей команды проверки
          model.Scheme = CommandsModel.CheckKeyP(model, model.Scheme, model.SiCommand);
        }
        else if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.С.ToString())
          || model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
        {
          model.Scheme = CommandsModel.CheckKeyS(model.Scheme);
        }
      }
      else if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.П.ToString())
        || model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
      {
        // находим цепи точек из предыдущей команды проверки
        model.Scheme = CommandsModel.CheckKeyP(model.SiCommand, model.Scheme);
      }
      else if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.С.ToString())
        || model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
      {
        model.Scheme = CommandsModel.CheckKeyS(model.Scheme);
      }
      else
      {
        // Во всём теле команды не нашли пары '*...*' → считаем, что точек нет
        LoggerUtility.LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {commandNumber} {mnemonic}");
        model.Errors.Add(IeErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }

      CheckUnparsedParameters(commandNumber, mnemonic, numberLine, model, remainder);

      LoggerUtility.LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
        
    private static void CheckUnparsedParameters(string commandNumber, string mnemonic, int numberLine, PiCommandModel model, string remainder)
    {
      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
      }
    }
  }
}
