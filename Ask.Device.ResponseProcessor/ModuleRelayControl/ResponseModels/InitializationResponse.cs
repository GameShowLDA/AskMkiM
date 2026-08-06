using System.Text.Json.Serialization;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;

/// <summary>
/// Содержит состояние МКР после выполнения команды инициализации.
/// </summary>
public sealed class InitializationResponse : ModuleRelayControlResponse
{
  /// <summary>
  /// Признак состояния МКР, отличающегося от исходного.
  /// </summary>
  [JsonPropertyName("NotDefaultState")]
  public bool NotDefaultState { get; init; }
}
