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
using DataBaseConfiguration.Services.Device;
using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser
{
  public class PointParser
  {
    /// <summary>
    /// Разбор блока точек '*...*' в SchemeModel.
    /// Правила:
    /// - Одна '*' внутри блока разделяет ЦЕПИ.
    /// - '#': части внутри одной цепи.
    /// - ',': перечисление токенов (точка или диапазон).
    /// - Диапазоны: 87-90, 1.2.7-10, 1.2.7-1.2.10, Х51/51-60.
    /// - Спец-кейс: токен, заканчивающийся '-' и следующий сегмент после '*' — конец диапазона
    ///   (пример 'Х51/51-*60') → каждая раскрытая точка = отдельная цепь.
    /// - Для КС: одиночная точка (один исходный токен БЕЗ '-') в части запрещена.
    /// </summary>
    public static (SchemeModel?, List<ErrorItem>) ParsePoints(string expr, BaseCommandModel model, RmCommandModel rmCommandModel)
    {
      if (rmCommandModel == null || rmCommandModel.PointsMap == null || rmCommandModel.PointsMap.Count == 0)
      {
        return (null, null);
      }

      var errors = new List<ErrorItem>();
      var chainModels = new List<GroupModel>();

      expr = Regex.Replace(expr ?? string.Empty, @"\s+", "");
      expr = expr.Trim('*');

      if (string.IsNullOrEmpty(expr))
        return (null, errors);

      const string RangeStarPlaceholder = "__RANGE_STAR__";

      expr = expr.Replace("-*", RangeStarPlaceholder);

      // Цепи теперь разделяются одной '*'
      var chainSegs = expr.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
      for (int i = 0; i < chainSegs.Length; i++)
      {
        chainSegs[i] = chainSegs[i].Replace(RangeStarPlaceholder, "-*");
      }

      for (int i = 0; i < chainSegs.Length; i++)
      {
        string chainSegOriginal = chainSegs[i];
        if (string.IsNullOrWhiteSpace(chainSegOriginal))
          continue;

        // Части внутри цепи по '#'
        var partTokens = chainSegOriginal.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(CleanToken)
                                         .Where(t => !string.IsNullOrEmpty(t))
                                         .ToList();

        // === Спец-кейс: одна часть, один токен, который заканчивается '-' и конец диапазона в СЛЕДУЮЩЕМ сегменте цепи ===
        if (partTokens.Count == 1)
        {
          var rawTokensSingle = partTokens[0].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(CleanToken)
                                             .Where(t => !string.IsNullOrEmpty(t))
                                             .ToList();

          if (rawTokensSingle.Count == 1 && rawTokensSingle[0].EndsWith("-") && (i + 1) < chainSegs.Length)
          {
            string leftWithDash = rawTokensSingle[0];                  
            string rightSeg = CleanToken(chainSegs[i + 1]);            
            // правый сегмент не должен содержать '#', ',' — иначе это не продолжение диапазона
            if (!string.IsNullOrEmpty(rightSeg)
                && !rightSeg.Contains('#')
                && !rightSeg.Contains(','))
            {
              string combinedRange = leftWithDash + rightSeg;          
              var expanded = ExpandRangeToken(combinedRange, errors);
              if (expanded.Count > 0)
              {
                // Каждая точка -> отдельная цепь
                foreach (var one in expanded)
                {
                  var (ok1, pts1) = CommandPostAnalyzer.GetPointsModel(new List<string> { one }, rmCommandModel.PointsMap);
                  foreach (var pts2 in pts1)
                  {
                    var pointMnemonic = rmCommandModel.PointsMap.Where(x => x.Value == pts2.ToString()).FirstOrDefault().Key;

                    pts2.PointType = PointType.Star;
                    pts2.Mnemonic = pointMnemonic;
                  }
                  var part = new ChainModel(new List<PointModel>(pts1 ?? new List<PointModel>()));
                  chainModels.Add(new GroupModel(new List<ChainModel> { part }));
                }
                i++; // съедаем следующий сегмент, т.к. он использован как конец диапазона
                continue; // переходим к следующей исходной "цепи"
              }
            }
          }
        }
        // === конец спец-кейса ===

        // Обычная обработка: каждая часть -> PartChainModel (после раскрытия диапазонов)
        var currentChainParts = new List<ChainModel>();
        var currentGroupParts = new List<GroupModel>();

        foreach (var part in partTokens)
        {
          var rawTokens = part.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(CleanToken)
                              .Where(t => !string.IsNullOrEmpty(t))
                              .ToList();

          // Раскрываем диапазоны
          var expandedTokens = new List<string>();
          var expandedDisconnectedTokens = new List<string>();
          for (int k = 0; k < rawTokens.Count; k++)
          {
            var tok = rawTokens[k];
            if (tok.Contains("-*"))
              expandedDisconnectedTokens.AddRange(ExpandRangeToken(tok, errors));
            else if (tok.Contains('-'))
              expandedTokens.AddRange(ExpandRangeToken(tok, errors));
            else
              expandedTokens.Add(tok);
          }

          // Проверка "одиночной точки" для КС: только если исходно был один токен без '-'
          bool isSingleOriginalToken = rawTokens.Count == 1 && !rawTokens[0].Contains('-');
          if (string.Equals(model.Mnemonic, "КС", StringComparison.OrdinalIgnoreCase)
              && isSingleOriginalToken
              && expandedTokens.Count == 1)
          {
            errors.Add(new ErrorItem
            {
              Description = $"Нельзя указывать одиночную точку (часть цепи содержит только: {expandedTokens[0]}).",
              Code = ErrorCode.Gen_InvalidOnePointUse
            });
          }

          var (ok2, pts2) = CommandPostAnalyzer.GetPointsModel(expandedTokens, rmCommandModel.PointsMap);
          currentChainParts.Add(new ChainModel(new List<PointModel>(pts2 ?? new List<PointModel>())));
          var (ok3, ptsDisconnected) = CommandPostAnalyzer.GetPointsModel(expandedDisconnectedTokens, rmCommandModel.PointsMap);
          foreach (var point in ptsDisconnected)
          {
            currentGroupParts.Add(new GroupModel(new List<ChainModel> { new ChainModel(new List<PointModel> { point }) }));
          }
          currentGroupParts.Add(new GroupModel(currentChainParts));
        }
        if (currentGroupParts.Count > 0 && currentChainParts.Count > 0)
        {
          if (rmCommandModel != null)
          {
            for (int gp = 0; gp < currentGroupParts.Count; gp++)
            {
              var group = currentGroupParts[gp];
              for (int cp = 0; cp < group.ChainModels.Count; cp++)
              {
                var chain = group.ChainModels[cp];
                for (int pm = 0; pm < chain.PointModels.Count; pm++)
                {
                  if (pm == 0 && cp == 0)
                  {
                    chain.PointModels[pm].PointType = PointType.Star;
                  }
                  else if (pm == 0 && cp > 0)
                  {
                    chain.PointModels[pm].PointType = PointType.Hash;
                  }
                  else
                  {
                    chain.PointModels[pm].PointType = PointType.Comma;
                  }

                  if (rmCommandModel.TryGetKeyByValue(chain.PointModels[pm].ToString(), out string key))
                  {
                    chain.PointModels[pm].Mnemonic = key;
                  }
                }
              }
              chainModels.Add(group);
            }
          }
        }
      }

      var count = 0;
      if (chainModels.Count > 0)
      {
        for (int i = 0; i < chainModels.Count; i++)
        {
          var points = new SchemeModel(chainModels).GetPointsDisconnected(chainModels[i]);
          if (points != null)
          {
            count++;
          }
        }
      }
      if (count < 2 && count != 0
                && (model.Mnemonic == EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PR).DisplayName
                || model.Mnemonic == EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.SI).DisplayName
                || model.Mnemonic == EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PI).DisplayName))
      {
        model.AlgorithmKey.Add(AlgorithmKey.ЗР.ToString());
        model.Warnings.Add(GeneralWarnings.KeyZR(model.StartLineNumber, $"{model.CommandNumber} {model.Mnemonic}"));
      }
      return (new SchemeModel(chainModels), errors);
    }

    /// <summary>
    /// Раскрывает диапазон: "87-90", "1.2.7-10", "1.2.7-1.2.10", "Х51/51-60".
    /// </summary>
    private static List<string> ExpandRangeToken(string token, List<ErrorItem> errors)
    {
      var result = new List<string>();

      token = CleanToken(token);
      token = Regex.Replace(token, @"-\*", "-");

      int dashIndex = token.IndexOf('-');
      if (dashIndex < 0)
        return result;

      string left = CleanToken(token.Substring(0, dashIndex));
      string right = CleanToken(token.Substring(dashIndex + 1));

      if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
      {
        errors.Add(new ErrorItem
        {
          Description = $"Неверный диапазон точек: {token}.",
          Code = ErrorCode.Gen_InvalidRange
        });
        return result;
      }

      // Выделяем префикс + старт (префикс заканчивается на '.' или '/')
      if (!TrySplitPrefixAndNumber(left, out string leftPrefix, out int startNum))
      {
        errors.Add(new ErrorItem
        {
          Description = $"Неверное начало диапазона: {left} (в {token}).",
          Code = ErrorCode.Gen_InvalidRange
        });
        return result;
      }

      // Определяем конец диапазона
      int endNum;
      if (TrySplitPrefixAndNumber(right, out string rightPrefix, out int rightNum))
      {
        if (!string.IsNullOrEmpty(rightPrefix) && rightPrefix != leftPrefix)
        {
          errors.Add(new ErrorItem
          {
            Description = $"Несовместимые префиксы в диапазоне: {token} (\"{leftPrefix}\" vs \"{rightPrefix}\").",
            Code = ErrorCode.Gen_InvalidRange
          });
          return result;
        }
        endNum = rightNum;
      }
      else
      {
        if (!int.TryParse(right, out endNum))
        {
          errors.Add(new ErrorItem
          {
            Description = $"Неверный конец диапазона: {right} (в {token}).",
            Code = ErrorCode.Gen_InvalidRange
          });
          return result;
        }
      }

      if (endNum < startNum)
      {
        errors.Add(new ErrorItem
        {
          Description = $"Неверный диапазон точек (конец меньше начала): {token}.",
          Code = ErrorCode.Gen_InvalidRange
        });
        return result;
      }

      for (int n = startNum; n <= endNum; n++)
        result.Add($"{leftPrefix}{n}");

      return result;
    }

    /// <summary>
    /// "префикс" + число. Префикс = всё до последнего '.' или '/' включительно.
    /// </summary>
    private static bool TrySplitPrefixAndNumber(string token, out string prefix, out int number)
    {
      prefix = "";
      number = 0;

      int dot = token.LastIndexOf('.');
      int slash = token.LastIndexOf('/');

      int sep = Math.Max(dot, slash);

      if (sep >= 0)
      {
        prefix = token.Substring(0, sep + 1);
        var tail = token.Substring(sep + 1);
        return int.TryParse(tail, out number);
      }
      else
      {
        return int.TryParse(token, out number);
      }
    }

    private static string CleanToken(string t)
    {
      if (t.Contains("-*"))
      {
        return t;
      }
      else
      {
        return string.IsNullOrEmpty(t) ? string.Empty : t.Replace("*", "").Trim();
      }
    }

    public static (Dictionary<SwitchingBus, List<PointModel>>, List<ErrorItem>) ParseBusPoints(
      string expr,
      RmCommandModel rmCommandModel,
      int lineNumber,
      string command)
    {
      if (rmCommandModel == null ||
          rmCommandModel.PointsMap == null ||
          rmCommandModel.PointsMap.Count == 0)
      {
        return (null, null);
      }

      var errors = new List<ErrorItem>();
      var buses = new Dictionary<SwitchingBus, List<PointModel>>();

      expr = Regex.Replace(expr ?? string.Empty, @"\s+", "");
      if (string.IsNullOrEmpty(expr))
        return (null, errors);

      var busSegments = expr.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

      foreach (var busSeg in busSegments)
      {
        // Формат: ШИНА:ТОЧКИ
        var parts = busSeg.Split(':');
        if (parts.Length != 2)
        {
          errors.Add(new ErrorItem
          {
            Description = $"Неверный формат описания шины: {busSeg}",
            Code = ErrorCode.Gen_InvalidRange
          });
          continue;
        }

        if (!BusConverter.TryParseSwitchingBus(parts[0], out var bus))
        {
          errors.Add(new ErrorItem
          {
            Description = $"Неизвестная шина: {parts[0]}",
            Code = ErrorCode.Gen_InvalidRange
          });
          continue;
        }

        string pointsPart = parts[1];

        var rawTokens = pointsPart
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanToken)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        // 5. Раскрываем диапазоны
        var expandedTokens = new List<string>();
        foreach (var tok in rawTokens)
        {
          if (tok.Contains("-"))
            expandedTokens.AddRange(ExpandRangeToken(tok, errors));
          else
            expandedTokens.Add(tok);
        }

        foreach (var token in expandedTokens)
        {
          if (!rmCommandModel.PointsMap.TryGetValue(token, out var address))
          {
            errors.Add(GeneralErrors.UnknownPoint(token, lineNumber, command));
            continue;
          }

          var point = PointModel.ParsePointString(address);
          point.Mnemonic = token;

          var realysModule = new RelaySwitchModuleServices().GetDevicesByNumberChassis(point.DeviceNumber).Where(x => x.Number == point.ModuleNumber).FirstOrDefault();

          if (realysModule == null)
          {
            errors.Add(new ErrorItem
            {
              Description = $"Модуль {point.DeviceNumber}.{point.ModuleNumber} не найден в конфигурации.",
              Code = ErrorCode.Gen_InvalidRange
            });
            continue;

          }

          BusConverter.TrySplitAbBus(realysModule.BusType, out SwitchingBus busA, out SwitchingBus busB);

          bool error = false;

          if (bus != busA && bus != busB)
          {
            errors.Add(new ErrorItem
            {
              Description = $"Модуль {realysModule.NumberChassis}.{realysModule.Number} не поддерживает шину {bus}",
              Code = ErrorCode.Gen_InvalidRange
            });

            error = true;
          }
          if (!error)
          {
            if (!buses.TryGetValue(bus, out var list))
            {
              list = new List<PointModel>();
              buses[bus] = list;
            }

            list.Add(point);
          }
        }
      }

      return (buses, errors);
    }


    public static List<SwitchingBus> ParseBusList(string expr, RmCommandModel rmCommandModel, int lineNumber, string command)
    {
      var errors = new List<ErrorItem>();
      var buses = new List<SwitchingBus>();

      expr = Regex.Replace(expr ?? string.Empty, @"\s+", "");
      if (string.IsNullOrEmpty(expr))
        return null;

      var busSegments = expr.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

      foreach (var busSeg in busSegments)
      {
        var parts = busSeg.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in parts)
        {
          if (BusConverter.TryParseSwitchingBus(item, out SwitchingBus bus))
          {
            buses.Add(bus);
          }
        }
      }
      return buses;
    }

    public static List<ParsedExprPart> ExtractSigns(string expr)
    {
      var result = new List<ParsedExprPart>();

      if (string.IsNullOrWhiteSpace(expr))
        return result;

      // убираем пробелы
      expr = Regex.Replace(expr, @"\s+", "");

      // разбиваем по '*'
      var segments = expr.Split('*', StringSplitOptions.RemoveEmptyEntries);

      foreach (var seg in segments)
      {
        if (string.IsNullOrWhiteSpace(seg))
          continue;

        char? sign = null;
        string body = seg;

        // знак ТОЛЬКО в начале сегмента
        if (seg[0] == '+' || seg[0] == '-')
        {
          sign = seg[0];
          body = seg.Substring(1);
        }

        if (string.IsNullOrWhiteSpace(body))
          continue;

        result.Add(new ParsedExprPart(
            CleanExpr: $"*{body}*",
            Sign: sign
        ));
      }

      return result;
    }


  }
}
