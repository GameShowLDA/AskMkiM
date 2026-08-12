using Ask.Device.ResponseProcessor.Multimeter.ResponseModels;
using System.Globalization;

namespace Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing.Checkers;

/// <summary>
/// Разбирает ответ мультиметра на команду запроса системной ошибки.
/// </summary>
internal static class InstrumentErrorResponseChecker
{
  /// <summary>
  /// Разбирает код и описание ошибки прибора.
  /// </summary>
  /// <param name="response">Ответ на команду <c>SYSTEM:ERROR?</c>.</param>
  /// <param name="result">Код и описание ошибки.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ имеет поддерживаемый формат.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool TryParse(string response, out InstrumentErrorResponse? result)
  {
    result = null;
    if (string.IsNullOrWhiteSpace(response))
    {
      return false;
    }

    string normalized = response.Trim();
    string[] parts = normalized.Split(',', 2);
    if (!int.TryParse(parts[0].Trim().TrimStart('+'), NumberStyles.Integer,
          CultureInfo.InvariantCulture, out int code))
    {
      return false;
    }

    result = new InstrumentErrorResponse
    {
      Code = code,
      Message = parts.Length == 2 ? parts[1].Trim().Trim('"') : string.Empty,
    };
    return true;
  }
}
