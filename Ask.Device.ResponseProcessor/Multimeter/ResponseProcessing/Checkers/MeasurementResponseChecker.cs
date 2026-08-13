using Ask.Device.ResponseProcessor.Multimeter.ResponseModels;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing.Checkers;

/// <summary>
/// Преобразует измерительные ответы мультиметра в числовые значения.
/// </summary>
internal static class MeasurementResponseChecker
{
  private const double ScpiOverloadThreshold = 9.9E+37;

  private static readonly HashSet<string> OverloadTokens = new(StringComparer.OrdinalIgnoreCase)
  {
    "OL",
    "OVL",
    "OVLD",
    "OVLOAD",
    "OVERLOAD"
  };

  /// <summary>
  /// Шаблон числа с необязательными знаком, дробной частью и экспонентой.
  /// </summary>
  private static readonly Regex NumericValuePattern = new(
    @"^[+-]?(?:\d+(?:[.,]\d*)?|[.,]\d+)(?:[eE][+-]?\d+)?",
    RegexOptions.Compiled | RegexOptions.CultureInvariant);

  /// <summary>
  /// Преобразует начало ответа мультиметра в числовое значение.
  /// </summary>
  /// <param name="response">Ответ мультиметра.</param>
  /// <param name="result">Результат преобразования ответа.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ содержит корректное число.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool TryParse(string response, out MeasurementResponse? result)
  {
    result = null;
    if (string.IsNullOrWhiteSpace(response))
    {
      return false;
    }

    string normalized = response.Trim();
    string token = normalized.Trim('"', '\'');
    if (OverloadTokens.Contains(token))
    {
      result = new MeasurementResponse
      {
        RawValue = response,
        Value = double.PositiveInfinity,
        State = MeasurementState.Overload
      };
      return true;
    }

    Match match = NumericValuePattern.Match(normalized);
    if (!match.Success || !double.TryParse(
          match.Value.Replace(',', '.'),
          NumberStyles.Float,
          CultureInfo.InvariantCulture,
          out double value))
    {
      return false;
    }

    result = new MeasurementResponse
    {
      RawValue = response,
      Value = value >= ScpiOverloadThreshold ? double.PositiveInfinity : value,
      State = value >= ScpiOverloadThreshold ? MeasurementState.Overload : MeasurementState.Value
    };
    return true;
  }
}
