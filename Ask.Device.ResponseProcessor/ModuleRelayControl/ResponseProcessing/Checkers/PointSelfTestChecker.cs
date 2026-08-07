using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;
using System.Text.Json;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет результат самоконтроля точки МКР.
/// </summary>
internal static class PointSelfTestChecker
{
  private const string SuccessfulStatus = "sucsess";

  /// <summary>
  /// Проверяет отправителя, номер точки и результаты всех этапов самоконтроля.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="chassisNumber">Ожидаемый номер шасси.</param>
  /// <param name="moduleNumber">Ожидаемый номер МКР в шасси.</param>
  /// <param name="pointNumber">Ожидаемый номер проверенной точки.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ принадлежит ожидаемому МКР, содержит ожидаемую точку
  /// и подтверждает успешное выполнение всех этапов самоконтроля.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool Check(
    string response,
    int chassisNumber,
    int moduleNumber,
    int pointNumber)
  {
    if (string.IsNullOrWhiteSpace(response))
    {
      return false;
    }

    try
    {
      PointSelfTestResponse? model = JsonSerializer.Deserialize<PointSelfTestResponse>(response);

      return model != null &&
        ModuleResponseIdentityChecker.Check(model, chassisNumber, moduleNumber) &&
        string.Equals(model.Status, SuccessfulStatus, StringComparison.OrdinalIgnoreCase) &&
        model.NumberPoint == pointNumber &&
        model.ConnectPoint &&
        model.DisconnectBusA &&
        model.DisconnectBusB &&
        model.SelfControl;
    }
    catch (JsonException)
    {
      return false;
    }
  }
}
