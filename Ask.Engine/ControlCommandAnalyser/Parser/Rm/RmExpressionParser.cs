using Ask.Core.Services.Errors.Translation;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Rm
{
  public static class RmExpressionParser
  {
    public static List<RmPairModel> ParseAllExpressions(string rmBlock, ref RmCommandModel baseCommandModel)
    {
      var result = new List<RmPairModel>();
      if (string.IsNullOrWhiteSpace(rmBlock))
      {
        baseCommandModel.Errors.Add(RmErrors.EmptyCommandBody(
          baseCommandModel.StartLineNumber,
          $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
        return result;
      }

      foreach (var expr in SplitExpressions(rmBlock, ref baseCommandModel))
      {
        ParseExpression(expr, result, ref baseCommandModel);
      }

      return result;
    }

    public static List<string> SplitExpressions(string input, ref RmCommandModel baseCommandModel)
    {
      var result = new List<string>();
      foreach (var rawLine in input.Replace("\r", string.Empty).Split('\n'))
      {
        var line = NormalizeSeparators(rawLine.Trim());
        if (string.IsNullOrWhiteSpace(line))
          continue;

        foreach (var token in SplitExpressionLine(line))
        {
          if (token.Contains('=') && !token.StartsWith("=") && !token.EndsWith("="))
          {
            result.Add(token);
          }
          else
          {
            baseCommandModel.Errors.Add(RmErrors.ExtraSpace(
              token,
              baseCommandModel.StartLineNumber,
              $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
          }
        }
      }

      return result;
    }

    public static List<string> ExpandAll(string expr, ref RmCommandModel baseCommandModel)
    {
      var result = new List<string>();
      expr = expr?.Trim() ?? string.Empty;
      if (expr.Length == 0)
        return result;

      LogDebug($"[RM] Expand: {expr}");

      if (expr.Contains('"') || expr.Contains('$'))
      {
        baseCommandModel.Errors.Add(RmErrors.UnacceptableSymbol(
          expr,
          baseCommandModel.StartLineNumber,
          $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
        return result;
      }

      var enumerated = SplitTopLevel(expr, ',');
      if (enumerated.Count > 1)
      {
        foreach (var item in enumerated)
          result.AddRange(ExpandAll(item, ref baseCommandModel));
        return result;
      }

      if (TryExpandDecimalCoordinateRange(expr, result))
        return result;

      if (TryExpandSlashExpression(expr, ref baseCommandModel, result))
        return result;

      if (TryExpandFullCompositeRange(expr, result))
        return result;

      if (TryExpandColonRange(expr, result))
        return result;

      if (TryExpandCompositeProduct(expr, result))
        return result;

      if (TryExpandComponent(expr, result))
        return result;

      result.Add(expr);
      return result;
    }

    public static List<string> ExpandRange(string prefix, string from, string to, int step = 1)
    {
      var result = new List<string>();
      if (step == 0)
        return result;

      if (TryExpandLetterRange(prefix, from, to, step, result))
        return result;

      if (int.TryParse(from, out int nFrom) && int.TryParse(to, out int nTo))
      {
        foreach (var number in Enumerate(nFrom, nTo, step))
          result.Add($"{prefix}{number}");
      }
      else
      {
        result.Add($"{prefix}{from}-{to}");
      }

      return result;
    }

    public static List<string> ExpandComponent(string part)
    {
      var result = new List<string>();
      if (TryExpandComponent(part, result))
        return result;

      return new List<string> { part.Trim() };
    }

    public static IEnumerable<List<string>> CartesianProduct(List<List<string>> sequences)
    {
      IEnumerable<List<string>> result = new List<List<string>> { new() };
      foreach (var sequence in sequences)
      {
        result =
          from seq in result
          from item in sequence
          select new List<string>(seq) { item };
      }

      return result;
    }

    private static void ParseExpression(string expr, List<RmPairModel> result, ref RmCommandModel baseCommandModel)
    {
      if (TryParseSynonymExpression(expr, out var left, out var middle, out var right))
      {
        ProcessExpression(left, middle, right, result, ref baseCommandModel);
      }
      else if (TryParseBasicExpression(expr, out left, out right))
      {
        ProcessExpression(left, null, right, result, ref baseCommandModel);
      }
      else
      {
        baseCommandModel.Errors.Add(RmErrors.CannotParseExpression(
          expr,
          baseCommandModel.StartLineNumber,
          $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
      }
    }

    private static bool TryParseSynonymExpression(string expr, out string left, out string middle, out string right)
    {
      var match = Regex.Match(expr, @"^(.*?)==(.+?)=(.+)$");
      if (match.Success)
      {
        left = match.Groups[1].Value.Trim();
        middle = match.Groups[2].Value.Trim();
        right = match.Groups[3].Value.Trim();
        return middle.Length > 0;
      }

      left = middle = right = null;
      return false;
    }

    private static bool TryParseBasicExpression(string expr, out string left, out string right)
    {
      var match = Regex.Match(expr, @"^(.*?)=(.+)$");
      if (match.Success)
      {
        left = match.Groups[1].Value.Trim();
        right = match.Groups[2].Value.Trim();
        return true;
      }

      left = right = null;
      return false;
    }

    private static void ProcessExpression(
      string left,
      string middle,
      string right,
      List<RmPairModel> result,
      ref RmCommandModel baseCommandModel)
    {
      if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
      {
        baseCommandModel.Errors.Add(RmErrors.EmptyLeftOrRight(
          left,
          middle,
          right,
          baseCommandModel.StartLineNumber,
          $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
        return;
      }

      var errorCount = baseCommandModel.Errors.Count;
      var leftList = ExpandAll(left, ref baseCommandModel);
      var rightList = ExpandAll(right, ref baseCommandModel);
      var middleList = string.IsNullOrWhiteSpace(middle)
        ? Enumerable.Repeat<string>(null, leftList.Count).ToList()
        : ExpandAll(middle, ref baseCommandModel);

      if (baseCommandModel.Errors.Count != errorCount)
        return;

      if (leftList.Count != rightList.Count)
      {
        baseCommandModel.Errors.Add(RmErrors.MismatchedCounts(
          leftList.Count,
          rightList.Count,
          baseCommandModel.StartLineNumber,
          $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
        return;
      }

      if (!string.IsNullOrWhiteSpace(middle) && middleList.Count != rightList.Count)
      {
        baseCommandModel.Errors.Add(RmErrors.MismatchedCounts(
          middleList.Count,
          rightList.Count,
          baseCommandModel.StartLineNumber,
          $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
        return;
      }

      for (int i = 0; i < leftList.Count; i++)
      {
        result.Add(new RmPairModel
        {
          OkPoint = leftList[i],
          Synonym = middleList[i],
          AskInput = rightList[i]
        });
      }
    }

    private static string NormalizeSeparators(string line)
    {
      line = Regex.Replace(line, @"\s*==\s*", "==");
      line = Regex.Replace(line, @"(?<!=)\s*=\s*(?!=)", "=");
      return line.Trim();
    }

    private static IEnumerable<string> SplitExpressionLine(string line)
    {
      foreach (var token in Regex.Split(line, @"\s+"))
      {
        if (!string.IsNullOrWhiteSpace(token))
          yield return token.Trim();
      }
    }

    private static bool TryExpandSlashExpression(string expr, ref RmCommandModel model, List<string> result)
    {
      var slash = expr.LastIndexOf('/');
      if (slash < 0 || slash == expr.Length - 1)
        return false;

      var prefix = expr.Substring(0, slash + 1);
      var tail = expr.Substring(slash + 1);
      if (!LooksExpandable(tail))
        return false;

      foreach (var item in ExpandAll(tail, ref model))
        result.Add(prefix + item);

      return true;
    }

    private static bool TryExpandFullCompositeRange(string expr, List<string> result)
    {
      if (expr.Contains('[') || expr.Contains(']'))
        return false;

      var match = Regex.Match(expr, @"^(?<left>.+)-(?<right>.+?)(?:\((?<step>-?\d+)\))?$");
      if (!match.Success)
        return false;

      var leftParts = match.Groups["left"].Value.Split('.');
      var rightParts = match.Groups["right"].Value.Split('.');
      var step = match.Groups["step"].Success ? int.Parse(match.Groups["step"].Value) : 1;

      if (step == 0)
        return false;

      if (rightParts.Length == 1 && leftParts.Length > 1 && int.TryParse(rightParts[0], out _))
      {
        rightParts = leftParts.Take(leftParts.Length - 1).Concat(rightParts).ToArray();
      }

      if (leftParts.Length != rightParts.Length || leftParts.Length < 2)
        return false;

      for (int i = 0; i < leftParts.Length - 1; i++)
      {
        if (!string.Equals(leftParts[i], rightParts[i], StringComparison.OrdinalIgnoreCase))
          return false;
      }

      if (!int.TryParse(leftParts[^1], out var from) || !int.TryParse(rightParts[^1], out var to))
        return false;

      var prefix = string.Join(".", leftParts.Take(leftParts.Length - 1));
      foreach (var number in Enumerate(from, to, step))
        result.Add($"{prefix}.{number}");

      return true;
    }

    private static bool TryExpandColonRange(string expr, List<string> result)
    {
      var match = Regex.Match(expr, @"^(?<prefix>.+:)(?<from>\d+)-(?<to>\d+)(?:\((?<step>-?\d+)\))?$");
      if (!match.Success)
        return false;

      var step = match.Groups["step"].Success ? int.Parse(match.Groups["step"].Value) : 1;
      result.AddRange(ExpandRange(
        match.Groups["prefix"].Value,
        match.Groups["from"].Value,
        match.Groups["to"].Value,
        step));
      return true;
    }

    private static bool TryExpandCompositeProduct(string expr, List<string> result)
    {
      var components = SplitTopLevel(expr, '.');
      if (components.Count <= 1 || !components.Any(LooksExpandable))
        return false;

      var expandedComponents = new List<List<string>>();
      foreach (var component in components)
      {
        var expanded = new List<string>();
        if (!TryExpandComponent(component, expanded))
          expanded.Add(component);
        expandedComponents.Add(expanded);
      }

      foreach (var tuple in CartesianProduct(expandedComponents))
        result.Add(string.Join(".", tuple));

      return true;
    }

    private static bool TryExpandComponent(string part, List<string> result)
    {
      part = part.Trim();
      if (part.Length == 0)
        return false;

      if (part.StartsWith("[") && part.EndsWith("]"))
      {
        foreach (var item in SplitTopLevel(part[1..^1], ','))
          result.Add(item);
        return true;
      }

      var letterRange = Regex.Match(part, @"^(?<prefix>[\p{L}]+)(?<from>\d+)-(?:(?<toPrefix>[\p{L}]+)?)(?<to>\d+)(?:\((?<step>-?\d+)\))?$");
      if (letterRange.Success)
      {
        var prefix = letterRange.Groups["prefix"].Value;
        var toPrefix = letterRange.Groups["toPrefix"].Value;
        if (toPrefix.Length > 0 && !string.Equals(prefix, toPrefix, StringComparison.OrdinalIgnoreCase))
          return false;

        var step = letterRange.Groups["step"].Success ? int.Parse(letterRange.Groups["step"].Value) : 1;
        result.AddRange(ExpandRange(prefix, letterRange.Groups["from"].Value, letterRange.Groups["to"].Value, step));
        return true;
      }

      var numericRange = Regex.Match(part, @"^(?<from>\d+)-(?<to>\d+)(?:\((?<step>-?\d+)\))?$");
      if (numericRange.Success)
      {
        var step = numericRange.Groups["step"].Success ? int.Parse(numericRange.Groups["step"].Value) : 1;
        result.AddRange(ExpandRange(string.Empty, numericRange.Groups["from"].Value, numericRange.Groups["to"].Value, step));
        return true;
      }

      return false;
    }

    private static bool TryExpandLetterRange(string prefix, string from, string to, int step, List<string> result)
    {
      var fromMatch = Regex.Match(from, @"^(?<letters>[\p{L}]+)(?<number>\d+)$");
      if (!fromMatch.Success)
        return false;

      var letters = fromMatch.Groups["letters"].Value;
      var fromNumber = int.Parse(fromMatch.Groups["number"].Value);
      int toNumber;

      var toMatch = Regex.Match(to, @"^(?<letters>[\p{L}]+)(?<number>\d+)$");
      if (toMatch.Success)
      {
        if (!string.Equals(letters, toMatch.Groups["letters"].Value, StringComparison.OrdinalIgnoreCase))
          return false;

        toNumber = int.Parse(toMatch.Groups["number"].Value);
      }
      else if (!int.TryParse(to, out toNumber))
      {
        return false;
      }

      foreach (var number in Enumerate(fromNumber, toNumber, step))
        result.Add($"{prefix}{letters}{number}");

      return true;
    }

    private static bool TryExpandDecimalCoordinateRange(string expr, List<string> result)
    {
      var match = Regex.Match(
        expr,
        @"^(?<prefix>[\p{L}]+)(?<start>\d+\.\d+)-(?<end>\d+\.\d+)\((?<step>\d+\.\d+)\)(?<suffix>/.+)$");

      if (!match.Success)
        return false;

      if (!decimal.TryParse(match.Groups["start"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var start)
          || !decimal.TryParse(match.Groups["end"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var end)
          || !decimal.TryParse(match.Groups["step"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var step)
          || step <= 0)
        return false;

      int precision = new[]
      {
        match.Groups["start"].Value,
        match.Groups["end"].Value,
        match.Groups["step"].Value
      }.Select(GetPrecision).Max();

      var format = "0." + new string('0', precision);
      for (var current = start; current <= end; current += step)
        result.Add($"{match.Groups["prefix"].Value}{current.ToString(format, CultureInfo.InvariantCulture)}{match.Groups["suffix"].Value}");

      return true;
    }

    private static int GetPrecision(string value)
    {
      var dot = value.IndexOf('.');
      return dot < 0 ? 0 : value.Length - dot - 1;
    }

    private static List<string> SplitTopLevel(string value, char separator)
    {
      var result = new List<string>();
      var depthSquare = 0;
      var depthRound = 0;
      var start = 0;

      for (int i = 0; i < value.Length; i++)
      {
        var ch = value[i];
        if (ch == '[') depthSquare++;
        else if (ch == ']') depthSquare--;
        else if (ch == '(') depthRound++;
        else if (ch == ')') depthRound--;
        else if (ch == separator && depthSquare == 0 && depthRound == 0)
        {
          result.Add(value[start..i].Trim());
          start = i + 1;
        }
      }

      result.Add(value[start..].Trim());
      return result.Where(x => x.Length > 0).ToList();
    }

    private static bool LooksExpandable(string value)
    {
      return value.Contains('-') || value.Contains('[') || value.Contains(',') || value.Contains('(');
    }

    private static IEnumerable<int> Enumerate(int from, int to, int step)
    {
      if (step == 0)
        yield break;

      var direction = to >= from ? 1 : -1;
      var actualStep = Math.Abs(step) * direction;
      for (int current = from; direction > 0 ? current <= to : current >= to; current += actualStep)
        yield return current;
    }
  }
}
