using System.Text.Json.Serialization;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;

/// <summary>
/// Содержит результат самоконтроля внешней шины МКР.
/// </summary>
public sealed class ExternalBusSelfTestResponse : ModuleRelayControlResponse
{
  /// <summary>
  /// Номер проверенной внешней шины.
  /// </summary>
  [JsonPropertyName("NumberBus")]
  public int NumberBus { get; init; }

  /// <summary>
  /// Номер защитного реле шины A.
  /// </summary>
  [JsonPropertyName("ProtectReleBusA")]
  public int ProtectRelayBusA { get; init; }

  /// <summary>
  /// Номер защитного реле шины B.
  /// </summary>
  [JsonPropertyName("ProtectReleBusB")]
  public int ProtectRelayBusB { get; init; }

  /// <summary>
  /// Результат проверки защитных реле.
  /// </summary>
  [JsonPropertyName("ConnectProtect")]
  public bool ProtectRelaysConnected { get; init; }

  /// <summary>
  /// Номер основного реле шины A.
  /// </summary>
  [JsonPropertyName("MainReleBusA")]
  public int MainRelayBusA { get; init; }

  /// <summary>
  /// Номер основного реле шины B.
  /// </summary>
  [JsonPropertyName("MainReleBusB")]
  public int MainRelayBusB { get; init; }

  /// <summary>
  /// Результат проверки основных реле.
  /// </summary>
  [JsonPropertyName("ConnectMain")]
  public bool MainRelaysConnected { get; init; }

  /// <summary>
  /// Числовой код последней ошибки прошивки.
  /// </summary>
  [JsonPropertyName("Error")]
  public int Error { get; init; }
}
