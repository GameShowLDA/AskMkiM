using System.Text.Json.Serialization;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;

/// <summary>
/// Содержит идентификационные поля ответа модуля коммутации реле.
/// </summary>
public class ModuleRelayControlResponse
{
  /// <summary>
  /// Имя модуля, заданное прошивкой.
  /// </summary>
  [JsonPropertyName("ModuleName")]
  public string? ModuleName { get; init; }

  /// <summary>
  /// Номер модуля в шасси.
  /// </summary>
  [JsonPropertyName("NumberDevice")]
  public int NumberDevice { get; init; }

  /// <summary>
  /// Номер шасси.
  /// </summary>
  [JsonPropertyName("NumberChassis")]
  public int NumberChassis { get; init; }
}
