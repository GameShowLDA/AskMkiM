using System.Text.Json.Serialization;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;

/// <summary>
/// Содержит подтверждение выполнения обычной команды МКР.
/// </summary>
public class CommandResponse : ModuleRelayControlResponse
{
  /// <summary>
  /// Строковое представление выполненной команды и её параметров.
  /// </summary>
  [JsonPropertyName("Answer")]
  public string? Answer { get; init; }

  /// <summary>
  /// Признак состояния МКР, отличающегося от исходного.
  /// </summary>
  [JsonPropertyName("NotDefaultState")]
  public bool NotDefaultState { get; init; }
}
