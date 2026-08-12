using System.Text.Json.Serialization;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;

/// <summary>
/// Содержит результат подключения или отключения точки с аппаратным контролем.
/// </summary>
public sealed class RelayVerificationResponse : CommandResponse
{
  /// <summary>
  /// Признак подтверждения требуемого состояния реле.
  /// </summary>
  [JsonPropertyName("Checked")]
  public bool Checked { get; init; }
}
