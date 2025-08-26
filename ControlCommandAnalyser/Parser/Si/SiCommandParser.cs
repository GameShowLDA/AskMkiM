using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Parser.Common;
using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model.Ok;
using Utilities; // Для LoggerUtility

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
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      if (lines == null || lines.Count == 0)
      {
        LoggerUtility.LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(SiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      for (int i = 0; i < model.SourceLines.ToList().Count; i++)
      {
        if (string.IsNullOrEmpty(model.SourceLines[i]) || string.IsNullOrWhiteSpace(model.SourceLines[i]))
        {
          model.SourceLines.Remove(model.SourceLines[i]);
          i--;
        }
      }

      // Первая строка — параметры (напряжение, сопротивление, время)
      var firstLine = lines[0];
      LoggerUtility.LogDebug($"Исходная первая строка: \"{firstLine}\"");

      var match = Regex.Match(firstLine, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      if (match.Success)
        firstLine = match.Groups[1].Value.Trim();

      string remainder = firstLine;
      string? voltage = null, resistance = null, time = null;

      // сначала извлекаем ключи
      var result = AlgorithmKeyParser.ExtractKeysWithTrailingCommaCheck(firstLine);

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

      (voltage, remainder) = CommonParameterParser.ParseVoltage(remainder);
      LoggerUtility.LogDebug($"После парсинга напряжения: voltage='{voltage}', remainder='{remainder}'");

      (resistance, remainder) = CommonParameterParser.ParseResistance(remainder);
      LoggerUtility.LogDebug($"После парсинга сопротивления: resistance='{resistance}', remainder='{remainder}'");

      (time, remainder) = CommonParameterParser.ParseTime(remainder);
      LoggerUtility.LogDebug($"После парсинга времени: time='{time}', remainder='{remainder}'");

      model.Voltage = voltage;
      model.Resistance = resistance;
      model.Time = time;

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

      var allPoints = new List<string>();
      int starIdx = remainder.IndexOf('*');
      if (starIdx >= 0)
      {
        string pointsPart = remainder.Substring(starIdx);
        LoggerUtility.LogDebug($"Парсинг точек из pointsPart: '{pointsPart}'");
        var pointsAndErrors = PointParser.ParsePoints(pointsPart, mnemonic);
        allPoints.AddRange(pointsAndErrors.Item1);
        if (pointsAndErrors.Item2.Count > 0)
        {
          foreach (var error in pointsAndErrors.Item2)
          {
            error.SourceLineNumber = numberLine;
            error.Command = $"{commandNumber} {mnemonic}";
            
            model.Errors.Add(error);
            LoggerUtility.LogError($"При парсинге точек команды {commandNumber} {mnemonic} произошла ошибка: {error.Description} (строка {error.SourceLineNumber}).");
          }
        }
        LoggerUtility.LogInformation($"Найдено точек в pointsPart: {allPoints.Count}");

        remainder = remainder.Substring(0, starIdx).Trim();
      }

      for (int i = 1; i < lines.Count; i++)
      {
        var pointLine = lines[i].Trim();
        if (!string.IsNullOrWhiteSpace(pointLine))
        {
          LoggerUtility.LogDebug($"Парсинг точек из строки {numberLine + i}: '{pointLine}'");
          var pointsBefore = allPoints.Count;
          var pointsAndErrors = PointParser.ParsePoints(pointLine, mnemonic);
          allPoints.AddRange(pointsAndErrors.Item1);
          if (pointsAndErrors.Item2.Count > 0)
          {
            foreach (var error in pointsAndErrors.Item2)
            {
              error.SourceLineNumber = numberLine;
              error.Command = $"{commandNumber} {mnemonic}";
              model.Errors.Add(error);
              LoggerUtility.LogError($"При парсинге точек команды {commandNumber} {mnemonic} произошла ошибка: {error.Description} (строка {error.SourceLineNumber}).");
            }
          }
          LoggerUtility.LogDebug($"Добавлено точек: {allPoints.Count - pointsBefore}");
        }
      }
      model.Points = allPoints;

      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
      }

      // Валидация
      if (string.IsNullOrWhiteSpace(voltage) && string.IsNullOrWhiteSpace(resistance) && string.IsNullOrWhiteSpace(time))
      {
        LoggerUtility.LogError($"Не удалось распознать параметры в строке: '{firstLine}' (строка {numberLine})");
        model.Errors.Add(SiErrors.CannotParseParameters(firstLine, numberLine, $"{commandNumber} {mnemonic}"));
      }

      if (model.Points.Count == 0)
      {
        LoggerUtility.LogWarning($"Не найдено ни одной точки (строка {numberLine}): {commandNumber} {mnemonic}");
        model.Errors.Add(SiErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }
      else
      {
        LoggerUtility.LogInformation($"Итого найдено точек: {model.Points.Count}");
      }

      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LoggerUtility.LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}