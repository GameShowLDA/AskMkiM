namespace Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseModels;

/// <summary>
/// Содержит адрес и подтверждение команды из JSON-ответа УКШ.
/// </summary>
internal sealed class DeviceBusCommutationResponse
{
  /// <summary>
  /// Имя модуля, заданное прошивкой УКШ.
  /// </summary>
  public string? ModuleName { get; init; }

  /// <summary>
  /// Номер УКШ в шасси.
  /// </summary>
  public int NumberDevice { get; init; }

  /// <summary>
  /// Номер шасси.
  /// </summary>
  public int NumberChassis { get; init; }

  /// <summary>
  /// Строковое подтверждение выполненной команды и её параметров.
  /// </summary>
  public string? Answer { get; init; }
}
