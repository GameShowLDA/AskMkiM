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
      if (!HasPointsMap(rmCommandModel))
        return (null, null);

      var errors = new List<ErrorItem>();
      var chainModels = new List<GroupModel>();

      expr = NormalizeExpression(expr);
      if (string.IsNullOrEmpty(expr))
        return (null, errors);

      var chainSegments = SplitChainSegments(expr);

      for (int i = 0; i < chainSegments.Count; i++)
      {
        var seg = chainSegments[i];
        if (string.IsNullOrWhiteSpace(seg))
          continue;

        if (TryHandleCrossSegmentRange(seg, chainSegments, ref i,
            rmCommandModel, chainModels, errors))
          continue;

        var groups = ParseChainParts(seg, model, rmCommandModel, errors);
        chainModels.AddRange(groups);
      }

      ApplyAlgorithmWarnings(chainModels, model);

      return (new SchemeModel(chainModels), errors);
    }

    /// <summary>
    /// Удаляет пробелы и внешние '*'.
    /// </summary>
    private static string NormalizeExpression(string expr)
    {
      expr = Regex.Replace(expr ?? string.Empty, @"\s+", "");
      return expr.Trim('*');
    }

    /// <summary>
    /// Проверяет наличие карты точек.
    /// </summary>
    private static bool HasPointsMap(RmCommandModel model) => model?.PointsMap != null && model.PointsMap.Count > 0;

    /// <summary>
    /// Делит выражение на сегменты цепей с учётом "-*".
    /// </summary>
    private static List<string> SplitChainSegments(string expr)
    {
      const string placeholder = "__RANGE_STAR__";

      expr = expr.Replace("-*", placeholder);

      var segs = expr
          .Split('*', StringSplitOptions.RemoveEmptyEntries)
          .Select(s => s.Replace(placeholder, "-*"))
          .ToList();

      return segs;
    }

    /// <summary>
    /// Обрабатывает диапазон, который продолжается в следующем сегменте.
    /// </summary>
    private static bool TryHandleCrossSegmentRange(string segment, List<string> allSegments, ref int index, RmCommandModel rm, List<GroupModel> chainModels, List<ErrorItem> errors)
    {
      var parts = SplitParts(segment);
      if (parts.Count != 1)
        return false;

      var tokens = SplitTokens(parts[0]);
      if (tokens.Count != 1 || !tokens[0].EndsWith("-") || index + 1 >= allSegments.Count)
        return false;

      var next = CleanToken(allSegments[index + 1]);
      if (next.Contains('#') || next.Contains(','))
        return false;

      var expanded = ExpandRangeToken(tokens[0] + next, errors);
      if (expanded.Count == 0)
        return false;

      foreach (var t in expanded)
        chainModels.Add(CreateSinglePointGroup(t, rm));

      index++;
      return true;
    }

    /// <summary>
    /// Разбирает сегмент цепи на группы.
    /// </summary>
    private static List<GroupModel> ParseChainParts(string segment, BaseCommandModel model, RmCommandModel rm, List<ErrorItem> errors)
    {
      var result = new List<GroupModel>();
      var chainParts = new List<ChainModel>();

      var parts = SplitParts(segment);

      foreach (var part in parts)
      {
        var (connected, disconnected) = ExpandTokens(part, errors);

        ValidateSinglePoint(model, connected, part, errors);

        var chain = CreateChain(connected, rm);
        chainParts.Add(chain);

        foreach (var d in disconnected)
          result.Add(CreateSinglePointGroup(d, rm));

        result.Add(new GroupModel(chainParts));
      }

      AssignPointTypes(result, rm);
      return result;
    }

    private static List<string> SplitParts(string seg) =>
      seg.Split('#', StringSplitOptions.RemoveEmptyEntries)
         .Select(CleanToken)
         .Where(x => !string.IsNullOrEmpty(x))
         .ToList();

    private static List<string> SplitTokens(string part) =>
      part.Split(',', StringSplitOptions.RemoveEmptyEntries)
          .Select(CleanToken)
          .Where(x => !string.IsNullOrEmpty(x))
          .ToList();

    /// <summary>
    /// Создаёт цепь из токенов.
    /// </summary>
    private static ChainModel CreateChain(List<string> tokens, RmCommandModel rm)
    {
      var (_, pts) = CommandPostAnalyzer.GetPointsModel(tokens, rm.PointsMap);
      return new ChainModel(pts?.ToList() ?? new List<PointModel>());
    }

    /// <summary>
    /// Создаёт группу из одной точки.
    /// </summary>
    private static GroupModel CreateSinglePointGroup(string token, RmCommandModel rm)
    {
      var (_, pts) = CommandPostAnalyzer.GetPointsModel(new List<string> { token }, rm.PointsMap);
      return new GroupModel(new List<ChainModel>
        {
          new ChainModel(pts?.ToList() ?? new List<PointModel>())
        });
    }

    /// <summary>
    /// Расставляет типы соединений и мнемоники.
    /// </summary>
    private static void AssignPointTypes(List<GroupModel> groups, RmCommandModel rm)
    {
      foreach (var group in groups)
      {
        for (int ci = 0; ci < group.ChainModels.Count; ci++)
        {
          var chain = group.ChainModels[ci];

          for (int pi = 0; pi < chain.PointModels.Count; pi++)
          {
            chain.PointModels[pi].PointType =
              pi == 0 && ci == 0 ? PointType.Star :
              pi == 0 ? PointType.Hash :
              PointType.Comma;

            if (rm.TryGetKeyByValue(chain.PointModels[pi].ToString(), out var key))
              chain.PointModels[pi].Mnemonic = key;
          }
        }
      }
    }

    /// <summary>
    /// Проверяет запрет одиночной точки для команды КС.
    /// </summary>
    private static void ValidateSinglePoint(BaseCommandModel model, List<string> expanded, string rawPart, List<ErrorItem> errors)
    {
      bool isSingle = !rawPart.Contains('-') && expanded.Count == 1;

      if (model.Mnemonic.Equals("КС", StringComparison.OrdinalIgnoreCase) && isSingle)
      {
        errors.Add(new ErrorItem
        {
          Description = $"Нельзя указывать одиночную точку ({expanded[0]}).",
          Code = ErrorCode.Gen_InvalidOnePointUse
        });
      }
    }

    /// <summary>
    /// Раскрывает токены части цепи:
    /// - обычные диапазоны → connected
    /// - диапазоны с "-*" → disconnected (отдельные группы)
    /// </summary>
    private static (List<string> Connected, List<string> Disconnected) ExpandTokens(string part, List<ErrorItem> errors)
    {
      var connected = new List<string>();
      var disconnected = new List<string>();

      var rawTokens = part.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(CleanToken)
                          .Where(t => !string.IsNullOrEmpty(t))
                          .ToList();

      foreach (var tok in rawTokens)
      {
        if (tok.Contains("-*"))
        {
          disconnected.AddRange(ExpandRangeToken(tok, errors));
        }
        else if (tok.Contains('-'))
        {
          connected.AddRange(ExpandRangeToken(tok, errors));
        }
        else
        {
          connected.Add(tok);
        }
      }

      return (connected, disconnected);
    }

    /// <summary>
    /// Раскрывает диапазон точек в список значений.
    /// </summary>
    private static List<string> ExpandRangeToken(string token, List<ErrorItem> errors)
    {
      var result = new List<string>();

      token = NormalizeRangeToken(token);

      if (!TrySplitRange(token, out var left, out var right))
        return result;

      if (!TryParseRangeBounds(token, left, right, errors,
          out string prefix, out int start, out int end))
        return result;

      if (!ValidateRangeBounds(token, start, end, errors))
        return result;

      return GenerateRangeValues(prefix, start, end);
    }

    /// <summary>
    /// Удаляет служебные символы и нормализует диапазон.
    /// </summary>
    private static string NormalizeRangeToken(string token)
    {
      token = CleanToken(token);
      return Regex.Replace(token, @"-\*", "-");
    }

    /// <summary>
    /// Делит диапазон на левую и правую части.
    /// </summary>
    private static bool TrySplitRange(string token, out string left, out string right)
    {
      left = right = string.Empty;

      int dashIndex = token.IndexOf('-');
      if (dashIndex < 0)
        return false;

      left = CleanToken(token.Substring(0, dashIndex));
      right = CleanToken(token.Substring(dashIndex + 1));

      return true;
    }

    /// <summary>
    /// Парсит начало и конец диапазона.
    /// </summary>
    private static bool TryParseRangeBounds(string token, string left, string right, List<ErrorItem> errors, out string prefix, out int start, out int end)
    {
      prefix = "";
      start = end = 0;

      if (!TrySplitPrefixAndNumber(left, out prefix, out start))
      {
        AddRangeError(errors, $"Неверное начало диапазона: {left} (в {token}).");
        return false;
      }

      if (TrySplitPrefixAndNumber(right, out string rightPrefix, out int rightNum))
      {
        if (!string.IsNullOrEmpty(rightPrefix) && rightPrefix != prefix)
        {
          AddRangeError(errors,
            $"Несовместимые префиксы в диапазоне: {token} (\"{prefix}\" vs \"{rightPrefix}\").");
          return false;
        }
        end = rightNum;
      }
      else if (!int.TryParse(right, out end))
      {
        AddRangeError(errors, $"Неверный конец диапазона: {right} (в {token}).");
        return false;
      }

      return true;
    }

    /// <summary>
    /// Проверяет корректность диапазона.
    /// </summary>
    private static bool ValidateRangeBounds(string token, int start, int end, List<ErrorItem> errors)
    {
      if (end < start)
      {
        AddRangeError(errors,
          $"Неверный диапазон точек (конец меньше начала): {token}.");
        return false;
      }

      return true;
    }

    /// <summary>
    /// Генерирует список значений диапазона.
    /// </summary>
    private static List<string> GenerateRangeValues(string prefix, int start, int end)
    {
      var result = new List<string>(end - start + 1);

      for (int n = start; n <= end; n++)
        result.Add($"{prefix}{n}");

      return result;
    }

    private static void AddRangeError(List<ErrorItem> errors, string message)
    {
      errors.Add(new ErrorItem
      {
        Description = message,
        Code = ErrorCode.Gen_InvalidRange
      });
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

    /// <summary>
    /// Парсит описание шин вида "*A:1,2,3*B:4-6".
    /// Возвращает точки, сгруппированные по шинам.
    /// </summary>
    public static (Dictionary<SwitchingBus, List<PointModel>>, List<ErrorItem>)
    ParseBusPoints(string expr, RmCommandModel rmCommandModel,
                   int lineNumber, string command)
    {
      if (!HasPointsMap(rmCommandModel))
        return (null, null);

      var errors = new List<ErrorItem>();
      var buses = new Dictionary<SwitchingBus, List<PointModel>>();

      expr = NormalizeExpression(expr);
      if (string.IsNullOrEmpty(expr))
        return (null, errors);

      var segments = SplitBusSegments(expr);

      foreach (var seg in segments)
      {
        if (!TryParseBusSegment(seg, out var bus, out var pointsPart, errors))
          continue;

        var tokens = ExpandBusTokens(pointsPart, errors);
        ProcessBusTokens(tokens, bus, rmCommandModel, buses, errors, lineNumber, command);
      }

      return (buses, errors);
    }

    /// <summary>
    /// Делит строку на сегменты вида "A:1,2".
    /// </summary>
    private static List<string> SplitBusSegments(string expr) =>
      expr.Split('*', StringSplitOptions.RemoveEmptyEntries).ToList();

    /// <summary>
    /// Парсит сегмент "ШИНА:ТОЧКИ".
    /// </summary>
    private static bool TryParseBusSegment(string segment, out SwitchingBus bus, out string pointsPart, List<ErrorItem> errors)
    {
      bus = default;
      pointsPart = null;

      var parts = segment.Split(':');
      if (parts.Length != 2)
      {
        AddBusError(errors, $"Неверный формат описания шины: {segment}");
        return false;
      }

      if (!BusConverter.TryParseSwitchingBus(parts[0], out bus))
      {
        AddBusError(errors, $"Неизвестная шина: {parts[0]}");
        return false;
      }

      pointsPart = parts[1];
      return true;
    }

    /// <summary>
    /// Разбивает точки и раскрывает диапазоны.
    /// </summary>
    private static List<string> ExpandBusTokens(string pointsPart, List<ErrorItem> errors)
    {
      var rawTokens = pointsPart
          .Split(',', StringSplitOptions.RemoveEmptyEntries)
          .Select(CleanToken)
          .Where(t => !string.IsNullOrEmpty(t))
          .ToList();

      var expanded = new List<string>();

      foreach (var tok in rawTokens)
      {
        if (tok.Contains('-'))
          expanded.AddRange(ExpandRangeToken(tok, errors));
        else
          expanded.Add(tok);
      }

      return expanded;
    }

    /// <summary>
    /// Создаёт PointModel и выполняет бизнес-валидацию.
    /// </summary>
    private static void ProcessBusTokens(List<string> tokens, SwitchingBus bus, RmCommandModel rm, Dictionary<SwitchingBus, List<PointModel>> buses,
      List<ErrorItem> errors, int lineNumber, string command)
    {
      foreach (var token in tokens)
      {
        if (!rm.PointsMap.TryGetValue(token, out var address))
        {
          errors.Add(GeneralErrors.UnknownPoint(token, lineNumber, command));
          continue;
        }

        var point = PointModel.ParsePointString(address);
        point.Mnemonic = token;

        if (!TryValidateBusSupport(point, bus, errors))
          continue;

        if (!buses.TryGetValue(bus, out var list))
        {
          list = new List<PointModel>();
          buses[bus] = list;
        }

        list.Add(point);
      }
    }

    /// <summary>
    /// Проверяет, поддерживает ли модуль указанную шину.
    /// </summary>
    private static bool TryValidateBusSupport(PointModel point, SwitchingBus bus, List<ErrorItem> errors)
    {
      var module = new RelaySwitchModuleServices()
          .GetDevicesByNumberChassis(point.DeviceNumber)
          .FirstOrDefault(x => x.Number == point.ModuleNumber);

      if (module == null)
      {
        AddBusError(errors,
          $"Модуль {point.DeviceNumber}.{point.ModuleNumber} не найден в конфигурации.");
        return false;
      }

      BusConverter.TrySplitAbBus(module.BusType, out var busA, out var busB);

      if (bus != busA && bus != busB)
      {
        AddBusError(errors,
          $"Модуль {module.NumberChassis}.{module.Number} не поддерживает шину {bus}");
        return false;
      }

      return true;
    }

    private static void AddBusError(List<ErrorItem> errors, string message)
    {
      errors.Add(new ErrorItem
      {
        Description = message,
        Code = ErrorCode.Gen_InvalidRange
      });
    }

    public static List<SwitchingBus> ParseBusList(string expr)
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

      expr = Regex.Replace(expr, @"\s+", "");
      var segments = expr.Split('*', StringSplitOptions.RemoveEmptyEntries);

      foreach (var seg in segments)
      {
        if (string.IsNullOrWhiteSpace(seg))
          continue;

        char? sign = null;
        string body = seg;

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

    /// <summary>
    /// Применяет бизнес-правило:
    /// если найдено меньше двух разомкнутых цепей для PR/SI/PI,
    /// автоматически добавляется алгоритм ЗР.
    /// </summary>
    private static void ApplyAlgorithmWarnings(List<GroupModel> chainModels, BaseCommandModel model)
    {
      if (chainModels == null || chainModels.Count == 0)
        return;

      int disconnectedCount = 0;
      var scheme = new SchemeModel(chainModels);

      foreach (var group in chainModels)
      {
        var points = scheme.GetPointsDisconnected(group);
        if (points != null)
          disconnectedCount++;
      }

      bool isMeasurementType =
         model.Mnemonic == EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PR).DisplayName ||
         model.Mnemonic == EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.SI).DisplayName ||
         model.Mnemonic == EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PI).DisplayName;

      if (disconnectedCount < 2 &&
          disconnectedCount != 0 &&
          isMeasurementType &&
          !model.AlgorithmKey.Contains(AlgorithmKey.ЗР.ToString()))
      {
        model.AlgorithmKey.Add(AlgorithmKey.ЗР.ToString());
        model.Warnings.Add(
          GeneralWarnings.KeyZR(model.StartLineNumber, $"{model.CommandNumber} {model.Mnemonic}")
        );
      }
    }
  }
}
