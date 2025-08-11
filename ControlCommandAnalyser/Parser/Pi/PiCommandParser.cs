using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Parser.Common;
using AppConfiguration.Error.Translation;
using Utilities; // Для LoggerUtility

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

      var model = new PiCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      if (lines == null || lines.Count == 0)
      {
        LoggerUtility.LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(PiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      var firstLine = lines[0];
      LoggerUtility.LogDebug($"Исходная первая строка: \"{firstLine}\"");

      var match = Regex.Match(firstLine, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      if (match.Success)
        firstLine = match.Groups[1].Value.Trim();

      string remainder = firstLine;
      string? voltage = null, thresholdResistance = null, time = null;

      // Парсим параметры
      (voltage, remainder) = CommonParameterParser.ParseVoltage(remainder);
      LoggerUtility.LogDebug($"После парсинга напряжения: voltage='{voltage}', remainder='{remainder}'");

      (thresholdResistance, remainder) = CommonParameterParser.ParseResistance(remainder);
      LoggerUtility.LogDebug($"После парсинга порогового сопротивления: threshold='{thresholdResistance}', remainder='{remainder}'");

      (time, remainder) = CommonParameterParser.ParseTime(remainder);
      LoggerUtility.LogDebug($"После парсинга времени: time='{time}', remainder='{remainder}'");

      model.Voltage = voltage;
      model.ThresholdResistance = thresholdResistance;
      model.Time = time;

      if (string.IsNullOrWhiteSpace(voltage))
      {
        model.Errors.Add(PiErrors.CannotParseParameters("Не указано напряжение", numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указано напряжение (строка {numberLine}): {commandNumber} {mnemonic}");
      }

      if (string.IsNullOrWhiteSpace(thresholdResistance))
      {
        model.Errors.Add(PiErrors.CannotParseParameters("Не указано пороговое сопротивление", numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указано пороговое сопротивление (строка {numberLine}): {commandNumber} {mnemonic}");
      }

      if (string.IsNullOrWhiteSpace(time))
      {
        model.Errors.Add(PiErrors.CannotParseParameters("Не указано время", numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указано время (строка {numberLine}): {commandNumber} {mnemonic}");
      }

      var allPoints = new List<string>();
      int starIdx = remainder.IndexOf('*');
      if (starIdx >= 0)
      {
        string pointsPart = remainder.Substring(starIdx);
        LoggerUtility.LogDebug($"Парсинг точек из pointsPart: '{pointsPart}'");
        allPoints.AddRange(PointParser.ParsePoints(pointsPart));
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
          allPoints.AddRange(PointParser.ParsePoints(pointLine));
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
      if (string.IsNullOrWhiteSpace(voltage) && string.IsNullOrWhiteSpace(thresholdResistance) && string.IsNullOrWhiteSpace(time))
      {
        LoggerUtility.LogError($"Не удалось распознать параметры в строке: '{firstLine}' (строка {numberLine})");
        model.Errors.Add(PiErrors.CannotParseParameters(firstLine, numberLine, $"{commandNumber} {mnemonic}"));
      }

      if (model.Points.Count == 0)
      {
        LoggerUtility.LogWarning($"Не найдено ни одной точки (строка {numberLine}): {commandNumber} {mnemonic}");
        model.Errors.Add(PiErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }
      else
      {
        LoggerUtility.LogInformation($"Итого найдено точек: {model.Points.Count}");
      }

      LoggerUtility.LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}
