using Ask.Device.ResponseProcessor.Multimeter.ResponseModels;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing.Checkers;

/// <summary>
/// Преобразует измерительные ответы мультиметра в числовые значения.
/// </summary>
internal static class MeasurementResponseChecker
{
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
    Match match = NumericValuePattern.Match(normalized);
    if (!match.Success || !double.TryParse(
          match.Value.Replace(',', '.'),
          NumberStyles.Float,
          CultureInfo.InvariantCulture,
          out double value))
    {
      return false;
    }

    result = new MeasurementResponse { RawValue = response, Value = value };
    return true;
  }
}
