using System.Text.Json.Serialization;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;

/// <summary>
/// Содержит статус обработки команды, возвращённый прошивкой МКР.
/// </summary>
public sealed class CommandStatusResponse : ModuleRelayControlResponse
{
  /// <summary>
  /// Исходный статус прошивки, включая значения <c>UnknownCommand</c>,
  /// <c>InvalidParametr</c> и <c>sucsess</c>.
  /// </summary>
  [JsonPropertyName("Status")]
  public string? Status { get; init; }
}
