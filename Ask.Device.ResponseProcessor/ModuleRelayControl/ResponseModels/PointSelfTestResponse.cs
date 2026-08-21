using System.Text.Json.Serialization;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;

/// <summary>
/// Содержит результат самоконтроля точки МКР.
/// </summary>
public sealed class PointSelfTestResponse : ModuleRelayControlResponse
{
  /// <summary>
  /// Статус выполнения самоконтроля, возвращённый прошивкой.
  /// </summary>
  [JsonPropertyName("Status")]
  public string? Status { get; init; }

  /// <summary>
  /// Номер проверенной точки.
  /// </summary>
  [JsonPropertyName("NumberPoint")]
  public int NumberPoint { get; init; }

  /// <summary>
  /// Признак успешного подключения точки к обеим шинам.
  /// </summary>
  [JsonPropertyName("ConnectPoint")]
  public bool ConnectPoint { get; init; }

  /// <summary>
  /// Признак успешного отключения точки от шины A.
  /// </summary>
  [JsonPropertyName("DisconnectBusA")]
  public bool DisconnectBusA { get; init; }

  /// <summary>
  /// Признак успешного отключения точки от шины B.
  /// </summary>
  [JsonPropertyName("DisconnectBusB")]
  public bool DisconnectBusB { get; init; }

  /// <summary>
  /// Итоговый результат самоконтроля точки.
  /// </summary>
  [JsonPropertyName("SelfControl")]
  public bool SelfControl { get; init; }
}
