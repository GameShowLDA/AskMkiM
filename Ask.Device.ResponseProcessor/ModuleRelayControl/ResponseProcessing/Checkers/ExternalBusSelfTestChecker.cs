using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;
using System.Text.Json;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет результат самоконтроля внешней шины МКР.
/// </summary>
internal static class ExternalBusSelfTestChecker
{
  /// <summary>
  /// Проверяет принадлежность ответа ожидаемой команде и успешность всех этапов самоконтроля.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="chassisNumber">Ожидаемый номер шасси.</param>
  /// <param name="moduleNumber">Ожидаемый номер МКР в шасси.</param>
  /// <param name="busNumber">Ожидаемый номер проверенной внешней шины.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ соответствует команде и подтверждает исправность шины.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool Check(string response, int chassisNumber, int moduleNumber, int busNumber)
  {
    ExternalBusSelfTestResponse? model = Deserialize(response);

    return MatchesRequest(model, chassisNumber, moduleNumber, busNumber) &&
      model!.ProtectRelaysConnected &&
      model.MainRelaysConnected &&
      model.Error == 0;
  }

  /// <summary>
  /// Проверяет идентификатор МКР, номер шины и назначенные ей номера реле.
  /// </summary>
  internal static bool MatchesRequest(
    ExternalBusSelfTestResponse? model,
    int chassisNumber,
    int moduleNumber,
    int busNumber)
  {
    return model != null &&
      ModuleResponseIdentityChecker.Check(model, chassisNumber, moduleNumber) &&
      model.NumberBus == busNumber &&
      model.ProtectRelayBusA == 100 + (busNumber * 2) - 1 &&
      model.ProtectRelayBusB == 108 + (busNumber * 2) - 1 &&
      model.MainRelayBusA == 100 + (busNumber * 2) &&
      model.MainRelayBusB == 108 + (busNumber * 2);
  }

  /// <summary>
  /// Десериализует JSON-ответ самоконтроля внешней шины.
  /// </summary>
  internal static ExternalBusSelfTestResponse? Deserialize(string response)
  {
    if (string.IsNullOrWhiteSpace(response))
    {
      return null;
    }

    try
    {
      return JsonSerializer.Deserialize<ExternalBusSelfTestResponse>(response);
    }
    catch (JsonException)
    {
      return null;
    }
  }
}
